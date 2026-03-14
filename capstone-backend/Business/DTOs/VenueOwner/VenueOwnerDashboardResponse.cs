namespace capstone_backend.Business.DTOs.VenueOwner;

/// <summary>
/// Response DTO cho venue owner dashboard overview
/// </summary>
public class VenueOwnerDashboardResponse
{
    // Overview Metrics
    public int TotalVenues { get; set; }
    public int ActiveVenues { get; set; }
    public int InactiveVenues { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalCheckIns { get; set; }
    public int TotalFavorites { get; set; }
    
    // Voucher Metrics
    public int TotalVouchers { get; set; }
    public int ActiveVouchers { get; set; }
    public int TotalVoucherExchanged { get; set; }
    public int TotalVoucherUsed { get; set; }
    public decimal VoucherExchangeRate { get; set; }
    public decimal VoucherUsageRate { get; set; }
    
    // Engagement Metrics
    public int TotalDatePlanInclusions { get; set; }
    public int TotalCollectionSaves { get; set; }
    public int UniqueCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    
    // Recent Activity
    public int NewReviewsThisWeek { get; set; }
    public int NewCheckInsThisWeek { get; set; }
    public int NewReviewsThisMonth { get; set; }
    public int NewCheckInsThisMonth { get; set; }
    
    // Growth Metrics (so với tháng trước)
    public decimal ReviewGrowthRate { get; set; }
    public decimal CheckInGrowthRate { get; set; }
    public decimal RatingTrend { get; set; }
    
    // Top Performing Venue
    public VenuePerformanceSummary? TopPerformingVenue { get; set; }
    
    // Venues List
    public List<VenuePerformanceSummary> Venues { get; set; } = new();
}

public class VenuePerformanceSummary
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Area { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int CheckInCount { get; set; }
    public int FavoriteCount { get; set; }
    public int DatePlanCount { get; set; }
    public int CollectionCount { get; set; }
    public string? CoverImage { get; set; }
}
