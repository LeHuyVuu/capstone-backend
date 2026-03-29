using capstone_backend.Business.Interfaces;
using capstone_backend.Api.VenueRecommendation.Service;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.VenueSubscription
{
    public class VenueSubscriptionWorker : IVenueSubscriptionWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VenueSubscriptionWorker> _logger;
        private readonly IMeilisearchService _meilisearchService;
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        public VenueSubscriptionWorker(
            IUnitOfWork unitOfWork,
            ILogger<VenueSubscriptionWorker> logger,
            IMeilisearchService meilisearchService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _meilisearchService = meilisearchService;
        }

        [JobDisplayName("Auto Expire Venue Subscriptions Daily")]
        public async Task AutoExpireVenueSubscriptionsDailyAsync()
        {
            var now = DateTime.UtcNow;

            var activeSubscriptions = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Where(vsp => vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString()
                    && vsp.EndDate.HasValue)
                .ToListAsync();

            var expiredSubscriptions = activeSubscriptions
                .Where(vsp => IsExpired(vsp.EndDate, now))
                .ToList();

            if (!expiredSubscriptions.Any())
            {
                _logger.LogInformation("[AUTO EXPIRE SUB] No expired active venue subscriptions found.");
                return;
            }

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = VenueSubscriptionPackageStatus.EXPIRED.ToString();
                subscription.UpdatedAt = now;
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
            }

            var venueIds = expiredSubscriptions
                .Where(s => s.VenueId.HasValue)
                .Select(s => s.VenueId!.Value)
                .Distinct()
                .ToList();

            var venueIdsToReindex = new HashSet<int>(venueIds);

            foreach (var venueId in venueIds)
            {
                var activeSubscriptionsForVenue = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                    .Where(vsp => vsp.VenueId == venueId
                        && vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString())
                    .ToListAsync();

                var hasAnyActiveSubscription = activeSubscriptionsForVenue
                    .Any(vsp => IsActiveAt(vsp.StartDate, vsp.EndDate, now));

                if (hasAnyActiveSubscription)
                {
                    continue;
                }

                var venue = await _unitOfWork.Context.Set<VenueLocation>()
                    .FirstOrDefaultAsync(v => v.Id == venueId && v.IsDeleted != true);

                if (venue != null && venue.Status == VenueLocationStatus.ACTIVE.ToString())
                {
                    venue.Status = VenueLocationStatus.INACTIVE.ToString();
                    venue.UpdatedAt = now;
                    _unitOfWork.Context.Set<VenueLocation>().Update(venue);
                }

                var activeVenueAds = await _unitOfWork.Context.Set<VenueLocationAdvertisement>()
                    .Where(vla => vla.VenueId == venueId
                        && vla.Status == VenueLocationAdvertisementStatus.ACTIVE.ToString())
                    .ToListAsync();

                foreach (var vla in activeVenueAds)
                {
                    vla.Status = VenueLocationAdvertisementStatus.EXPIRED.ToString();
                    vla.UpdatedAt = now;
                    _unitOfWork.Context.Set<VenueLocationAdvertisement>().Update(vla);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var venueId in venueIdsToReindex)
            {
                try
                {
                    await _meilisearchService.IndexVenueLocationAsync(venueId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[AUTO EXPIRE SUB] Failed to reindex venue {VenueId} to Meilisearch after subscription expiry",
                        venueId);
                }
            }

        }

        private static bool IsActiveAt(DateTime? startDate, DateTime? endDate, DateTime referenceUtc)
        {
            var normalizedStart = NormalizeBoundaryToUtc(startDate);
            var normalizedEnd = NormalizeBoundaryToUtc(endDate);

            var started = !normalizedStart.HasValue || normalizedStart.Value <= referenceUtc;
            var notEnded = !normalizedEnd.HasValue || normalizedEnd.Value >= referenceUtc;

            return started && notEnded;
        }

        private static bool IsExpired(DateTime? endDate, DateTime referenceUtc)
        {
            if (!endDate.HasValue)
            {
                return false;
            }

            var normalizedEnd = NormalizeBoundaryToUtc(endDate);
            return normalizedEnd.HasValue && normalizedEnd.Value < referenceUtc;
        }

        private static DateTime? NormalizeBoundaryToUtc(DateTime? boundary)
        {
            if (!boundary.HasValue)
            {
                return null;
            }

            var value = boundary.Value;

            if (value.Kind == DateTimeKind.Utc)
            {
                return value;
            }

            if (value.Kind == DateTimeKind.Local)
            {
                return value.ToUniversalTime();
            }

            // Business timestamps are entered in VN time (+07) and can be persisted without kind metadata.
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), VietnamTimeZone);
        }

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
        }
    }
}
