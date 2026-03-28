using capstone_backend.Business.Interfaces;
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

        public VenueSubscriptionWorker(IUnitOfWork unitOfWork, ILogger<VenueSubscriptionWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [JobDisplayName("Auto Expire Venue Subscriptions Daily")]
        public async Task AutoExpireVenueSubscriptionsDailyAsync()
        {
            var now = DateTime.UtcNow;

            var expiredSubscriptions = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Where(vsp => vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString()
                    && vsp.EndDate.HasValue
                    && vsp.EndDate.Value < now)
                .ToListAsync();

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

            foreach (var venueId in venueIds)
            {
                var hasAnyActiveSubscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                    .AnyAsync(vsp => vsp.VenueId == venueId
                        && vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString()
                        && (!vsp.StartDate.HasValue || vsp.StartDate.Value <= now)
                        && (!vsp.EndDate.HasValue || vsp.EndDate.Value >= now));

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

            _logger.LogInformation("[AUTO EXPIRE SUB] Processed {Count} expired venue subscription(s)", expiredSubscriptions.Count);
        }
    }
}
