using capstone_backend.Business.DTOs.VenueOwner;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class VenueOwnerDashboardService : IVenueOwnerDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VenueOwnerDashboardService> _logger;

    public VenueOwnerDashboardService(
        IUnitOfWork unitOfWork,
        ILogger<VenueOwnerDashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VenueOwnerDashboardResponse> GetDashboardOverviewAsync(int userId)
    {
        // Get venue owner profile
        var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
        if (venueOwner == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy hồ sơ chủ địa điểm");
        }

        // Get all venues của owner
        var venues = await _unitOfWork.VenueLocations.GetByVenueOwnerIdAsync(venueOwner.Id);
        var venueIds = venues.Select(v => v.Id).ToList();

        if (!venueIds.Any())
        {
            return new VenueOwnerDashboardResponse
            {
                TotalVenues = 0,
                Venues = new List<VenuePerformanceSummary>()
            };
        }

        // Calculate date ranges
        var now = DateTime.UtcNow;
        var oneWeekAgo = now.AddDays(-7);
        var oneMonthAgo = now.AddDays(-30);
        var twoMonthsAgo = now.AddDays(-60);

        // Get all reviews for these venues
        var allReviews = await _unitOfWork.Context.Set<Data.Entities.Review>()
            .Where(r => venueIds.Contains(r.VenueId) && r.IsDeleted != true)
            .ToListAsync();

        // Get all check-ins
        var allCheckIns = await _unitOfWork.Context.Set<Data.Entities.CheckInHistory>()
            .Where(c => venueIds.Contains(c.VenueId))
            .ToListAsync();

        // Get all vouchers
        var allVouchers = await _unitOfWork.Context.Set<Data.Entities.Voucher>()
            .Include(v => v.VoucherItems)
            .Include(v => v.VoucherLocations)
            .Where(v => v.VenueOwnerId == venueOwner.Id && v.IsDeleted != true)
            .ToListAsync();

        // Get date plan inclusions
        var datePlanInclusions = await _unitOfWork.Context.Set<Data.Entities.DatePlanItem>()
            .Where(dpi => venueIds.Contains(dpi.VenueLocationId) && dpi.IsDeleted != true)
            .CountAsync();

        // Get collection saves
        var collectionSaves = await _unitOfWork.Context.Set<Data.Entities.Collection>()
            .Where(c => c.Venues.Any(v => venueIds.Contains(v.Id)) && c.IsDeleted != true)
            .CountAsync();

        // Calculate metrics
        var totalReviews = allReviews.Count;
        var totalCheckIns = allCheckIns.Count;
        var averageRating = totalReviews > 0 ? allReviews.Average(r => r.Rating ?? 0) : 0;

        // Recent activity
        var reviewsThisWeek = allReviews.Count(r => r.CreatedAt >= oneWeekAgo);
        var checkInsThisWeek = allCheckIns.Count(c => c.CreatedAt >= oneWeekAgo);
        var reviewsThisMonth = allReviews.Count(r => r.CreatedAt >= oneMonthAgo);
        var checkInsThisMonth = allCheckIns.Count(c => c.CreatedAt >= oneMonthAgo);

        // Growth calculation
        var reviewsLastMonth = allReviews.Count(r => r.CreatedAt >= twoMonthsAgo && r.CreatedAt < oneMonthAgo);
        var checkInsLastMonth = allCheckIns.Count(c => c.CreatedAt >= twoMonthsAgo && c.CreatedAt < oneMonthAgo);

        var reviewGrowth = reviewsLastMonth > 0 
            ? ((decimal)(reviewsThisMonth - reviewsLastMonth) / reviewsLastMonth) * 100 
            : 0;
        var checkInGrowth = checkInsLastMonth > 0 
            ? ((decimal)(checkInsThisMonth - checkInsLastMonth) / checkInsLastMonth) * 100 
            : 0;

        // Voucher metrics
        var totalVoucherItems = allVouchers.SelectMany(v => v.VoucherItems).ToList();
        var exchangedVouchers = totalVoucherItems.Count(vi => vi.Status != VoucherItemStatus.AVAILABLE.ToString());
        var usedVouchers = totalVoucherItems.Count(vi => vi.Status == VoucherItemStatus.USED.ToString());
        
        var exchangeRate = totalVoucherItems.Count > 0 
            ? (decimal)exchangedVouchers / totalVoucherItems.Count * 100 
            : 0;
        var usageRate = exchangedVouchers > 0 
            ? (decimal)usedVouchers / exchangedVouchers * 100 
            : 0;

        // Unique customers
        var uniqueCustomers = allCheckIns.Select(c => c.MemberId).Distinct().Count();
        var returningCustomers = allCheckIns
            .GroupBy(c => c.MemberId)
            .Count(g => g.Count() > 1);

        // Advertisement metrics
        var allAdvertisements = await _unitOfWork.Context.Set<Data.Entities.Advertisement>()
            .Include(a => a.VenueLocationAdvertisements)
            .Where(a => a.VenueOwnerId == venueOwner.Id && a.IsDeleted != true)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var recentAds = allAdvertisements
            .Take(5)
            .Select(a => new AdvertisementSummary
            {
                Id = a.Id,
                Title = a.Title,
                BannerUrl = a.BannerUrl,
                PlacementType = a.PlacementType,
                Status = a.Status,
                Category = a.Category,
                DesiredStartDate = a.DesiredStartDate,
                CreatedAt = a.CreatedAt,
                VenueCount = a.VenueLocationAdvertisements.Count
            })
            .ToList();

        // Build venue performance summaries
        var venuePerformances = new List<VenuePerformanceSummary>();
        foreach (var venue in venues)
        {
            var venueReviews = allReviews.Where(r => r.VenueId == venue.Id).ToList();
            var venueCheckIns = allCheckIns.Count(c => c.VenueId == venue.Id);
            var venueDatePlans = await _unitOfWork.Context.Set<Data.Entities.DatePlanItem>()
                .CountAsync(dpi => dpi.VenueLocationId == venue.Id && dpi.IsDeleted != true);
            var venueCollections = await _unitOfWork.Context.Set<Data.Entities.Collection>()
                .CountAsync(c => c.Venues.Any(v => v.Id == venue.Id) && c.IsDeleted != true);

            venuePerformances.Add(new VenuePerformanceSummary
            {
                VenueId = venue.Id,
                VenueName = venue.Name,
                Category = venue.Category,
                Area = venue.Area,
                Status = venue.Status ?? "UNKNOWN",
                AverageRating = venue.AverageRating,
                ReviewCount = venueReviews.Count,
                CheckInCount = venueCheckIns,
                FavoriteCount = venue.FavoriteCount ?? 0,
                DatePlanCount = venueDatePlans,
                CollectionCount = venueCollections,
                CoverImage = venue.CoverImage
            });
        }

        // Find top performing venue (by engagement score)
        var topVenue = venuePerformances
            .OrderByDescending(v => (v.ReviewCount * 3) + (v.CheckInCount * 2) + v.FavoriteCount + v.DatePlanCount)
            .FirstOrDefault();

        return new VenueOwnerDashboardResponse
        {
            // Overview
            TotalVenues = venues.Count,
            ActiveVenues = venues.Count(v => v.Status == VenueLocationStatus.ACTIVE.ToString()),
            InactiveVenues = venues.Count(v => v.Status != VenueLocationStatus.ACTIVE.ToString()),
            AverageRating = (decimal)averageRating,
            TotalReviews = totalReviews,
            TotalCheckIns = totalCheckIns,
            TotalFavorites = venues.Sum(v => v.FavoriteCount ?? 0),

            // Voucher
            TotalVouchers = allVouchers.Count,
            ActiveVouchers = allVouchers.Count(v => v.Status == VoucherStatus.ACTIVE.ToString()),
            TotalVoucherExchanged = exchangedVouchers,
            TotalVoucherUsed = usedVouchers,
            VoucherExchangeRate = exchangeRate,
            VoucherUsageRate = usageRate,

            // Engagement
            TotalDatePlanInclusions = datePlanInclusions,
            TotalCollectionSaves = collectionSaves,
            UniqueCustomers = uniqueCustomers,
            ReturningCustomers = returningCustomers,

            // Recent Activity
            NewReviewsThisWeek = reviewsThisWeek,
            NewCheckInsThisWeek = checkInsThisWeek,
            NewReviewsThisMonth = reviewsThisMonth,
            NewCheckInsThisMonth = checkInsThisMonth,

            // Growth
            ReviewGrowthRate = reviewGrowth,
            CheckInGrowthRate = checkInGrowth,
            RatingTrend = 0, // TODO: Calculate rating trend

            // Advertisement
            TotalAdvertisements = allAdvertisements.Count,
            ActiveAdvertisements = allAdvertisements.Count(a => a.Status == "ACTIVE"),
            PendingAdvertisements = allAdvertisements.Count(a => a.Status == "PENDING"),
            RejectedAdvertisements = allAdvertisements.Count(a => a.Status == "REJECTED"),
            RecentAdvertisements = recentAds,

            // Top venue
            TopPerformingVenue = topVenue,

            // All venues
            Venues = venuePerformances.OrderByDescending(v => v.ReviewCount).ToList()
        };
    }

    public async Task<VenueAnalyticsResponse> GetVenueAnalyticsAsync(int userId, int venueId, int days = 30)
    {
        // Verify ownership
        var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
        if (venueOwner == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy hồ sơ chủ địa điểm");
        }

        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
        if (venue == null || venue.VenueOwnerId != venueOwner.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem địa điểm này");
        }

        var startDate = DateTime.UtcNow.AddDays(-days);

        // Get reviews
        var reviews = await _unitOfWork.Context.Set<Data.Entities.Review>()
            .Include(r => r.Member)
                .ThenInclude(m => m.User)
            .Where(r => r.VenueId == venueId && r.IsDeleted != true)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        // Get check-ins
        var checkIns = await _unitOfWork.Context.Set<Data.Entities.CheckInHistory>()
            .Where(c => c.VenueId == venueId && c.CreatedAt >= startDate)
            .ToListAsync();

        // Rating distribution
        var ratingStats = new RatingDistribution
        {
            FiveStars = reviews.Count(r => r.Rating == 5),
            FourStars = reviews.Count(r => r.Rating == 4),
            ThreeStars = reviews.Count(r => r.Rating == 3),
            TwoStars = reviews.Count(r => r.Rating == 2),
            OneStar = reviews.Count(r => r.Rating == 1),
            AverageRating = reviews.Any() ? (decimal)reviews.Average(r => r.Rating ?? 0) : 0,
            TotalReviews = reviews.Count,
            ReviewsWithPhotos = reviews.Count(r => !string.IsNullOrEmpty(r.ImageUrls)),
            ReviewsFromCouples = reviews.Count(r => r.CoupleProfileId.HasValue)
        };

        // Review trend (group by date)
        var reviewTrend = reviews
            .Where(r => r.CreatedAt >= startDate)
            .GroupBy(r => r.CreatedAt!.Value.Date)
            .Select(g => new TimeSeriesData
            {
                Date = g.Key,
                Count = g.Count(),
                AverageValue = (decimal?)g.Average(r => r.Rating ?? 0)
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Check-in trend
        var checkInTrend = checkIns
            .GroupBy(c => c.CreatedAt!.Value.Date)
            .Select(g => new TimeSeriesData
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Customer insights
        var uniqueCustomerIds = checkIns.Select(c => c.MemberId).Distinct().ToList();
        var returningCount = checkIns
            .GroupBy(c => c.MemberId)
            .Count(g => g.Count() > 1);
        var coupleReviewCount = reviews.Count(r => r.CoupleProfileId.HasValue);

        var customerStats = new CustomerInsights
        {
            TotalUniqueCustomers = uniqueCustomerIds.Count,
            ReturningCustomers = returningCount,
            CoupleCustomers = coupleReviewCount,
            SingleCustomers = reviews.Count - coupleReviewCount,
            ReturnRate = uniqueCustomerIds.Count > 0 ? (decimal)returningCount / uniqueCustomerIds.Count * 100 : 0,
            CoupleRate = reviews.Count > 0 ? (decimal)coupleReviewCount / reviews.Count * 100 : 0
        };

        // Peak hours (from check-ins)
        var peakHours = checkIns
            .Where(c => c.CreatedAt.HasValue)
            .GroupBy(c => c.CreatedAt!.Value.Hour)
            .Select(g => new PeakHourData
            {
                Hour = g.Key,
                CheckInCount = g.Count(),
                TimeLabel = $"{g.Key:D2}:00"
            })
            .OrderByDescending(p => p.CheckInCount)
            .Take(10)
            .ToList();

        // Recent reviews
        var recentReviews = reviews
            .Take(10)
            .Select(r => new RecentReviewSummary
            {
                ReviewId = r.Id,
                MemberName = r.IsAnonymous == true ? "Anonymous" : (r.Member?.FullName ?? "Unknown"),
                Rating = r.Rating ?? 0,
                Content = r.Content,
                CreatedAt = r.CreatedAt ?? DateTime.UtcNow,
                LikeCount = r.LikeCount ?? 0,
                HasPhotos = !string.IsNullOrEmpty(r.ImageUrls),
                IsFromCouple = r.CoupleProfileId.HasValue
            })
            .ToList();

        // Voucher performance for this venue
        var venueVouchers = await _unitOfWork.Context.Set<Data.Entities.Voucher>()
            .Include(v => v.VoucherItems)
            .Include(v => v.VoucherLocations)
            .Where(v => v.VenueOwnerId == venueOwner.Id 
                     && v.VoucherLocations.Any(vl => vl.VenueLocationId == venueId)
                     && v.IsDeleted != true)
            .ToListAsync();

        var voucherItems = venueVouchers.SelectMany(v => v.VoucherItems).ToList();
        var exchanged = voucherItems.Count(vi => vi.Status != VoucherItemStatus.AVAILABLE.ToString());
        var used = voucherItems.Count(vi => vi.Status == VoucherItemStatus.USED.ToString());

        var topVouchers = venueVouchers
            .Select(v => new TopVoucherSummary
            {
                VoucherId = v.Id,
                Title = v.Title ?? v.Code,
                ExchangedCount = v.VoucherItems.Count(vi => vi.Status != VoucherItemStatus.AVAILABLE.ToString()),
                UsedCount = v.VoucherItems.Count(vi => vi.Status == VoucherItemStatus.USED.ToString()),
                Status = v.Status ?? "UNKNOWN"
            })
            .OrderByDescending(v => v.ExchangedCount)
            .Take(5)
            .ToList();

        var voucherStats = new VoucherPerformance
        {
            TotalVouchers = venueVouchers.Count,
            ActiveVouchers = venueVouchers.Count(v => v.Status == VoucherStatus.ACTIVE.ToString()),
            TotalExchanged = exchanged,
            TotalUsed = used,
            ExchangeRate = voucherItems.Count > 0 ? (decimal)exchanged / voucherItems.Count * 100 : 0,
            UsageRate = exchanged > 0 ? (decimal)used / exchanged * 100 : 0,
            TopVouchers = topVouchers
        };

        return new VenueAnalyticsResponse
        {
            VenueId = venue.Id,
            VenueName = venue.Name,
            Category = venue.Category,
            Area = venue.Area,
            RatingStats = ratingStats,
            ReviewTrend = reviewTrend,
            CheckInTrend = checkInTrend,
            CustomerStats = customerStats,
            PeakHours = peakHours,
            RecentReviews = recentReviews,
            VoucherStats = voucherStats
        };
    }
}


