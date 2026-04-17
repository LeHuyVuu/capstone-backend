namespace capstone_backend.Business.DTOs.VenueOwner;

/// <summary>
/// Response DTO cho chi tiết analytics của 1 venue
/// </summary>
public class VenueAnalyticsResponse
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Area { get; set; }
    
    // Rating Distribution
    public RatingDistribution RatingStats { get; set; } = new();
    
    // Time-series Data
    public List<TimeSeriesData> ReviewTrend { get; set; } = new();
    public List<TimeSeriesData> CheckInTrend { get; set; } = new();
    
    // Customer Insights
    public CustomerInsights CustomerStats { get; set; } = new();
    
    // Peak Hours
    public List<PeakHourData> PeakHours { get; set; } = new();
    
    // Recent Reviews
    public List<RecentReviewSummary> RecentReviews { get; set; } = new();
    
    // Voucher Performance
    public VoucherPerformance VoucherStats { get; set; } = new();
}

public class RatingDistribution
{
    public int FiveStars { get; set; }
    public int FourStars { get; set; }
    public int ThreeStars { get; set; }
    public int TwoStars { get; set; }
    public int OneStar { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int ReviewsWithPhotos { get; set; }
}

public class TimeSeriesData
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal? AverageValue { get; set; }
}

public class CustomerInsights
{
    public int TotalUniqueCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    public decimal ReturnRate { get; set; }
}

public class PeakHourData
{
    public int Hour { get; set; }
    public int CheckInCount { get; set; }
    public string TimeLabel { get; set; } = string.Empty;
}

public class RecentReviewSummary
{
    public int ReviewId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool HasPhotos { get; set; }
}

public class VoucherPerformance
{
    public int TotalVouchers { get; set; }
    public int ActiveVouchers { get; set; }
    public int TotalExchanged { get; set; }
    public int TotalUsed { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal UsageRate { get; set; }
    public List<TopVoucherSummary> TopVouchers { get; set; } = new();
}

public class TopVoucherSummary
{
    public int VoucherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ExchangedCount { get; set; }
    public int UsedCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
