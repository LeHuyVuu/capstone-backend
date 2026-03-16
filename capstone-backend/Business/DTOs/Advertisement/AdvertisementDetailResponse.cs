namespace capstone_backend.Business.DTOs.Advertisement;

public class AdvertisementDetailResponse
{
    public int Id { get; set; }
    public int VenueOwnerId { get; set; }
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public string BannerUrl { get; set; } = null!;
    public string? TargetUrl { get; set; }
    public string PlacementType { get; set; } = null!;
    public string Status { get; set; } = null!; // DRAFT, PENDING, ACTIVE, REJECTED, EXPIRED
    public List<RejectionHistoryEntry>? RejectionHistory { get; set; }
    public DateTime? DesiredStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Related venue location advertisements
    public List<VenueLocationAdInfo>? VenueLocationAds { get; set; }
    
    // Related ads orders
    public List<AdsOrderInfo>? AdsOrders { get; set; }
}

public class VenueLocationAdInfo
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public int? PriorityScore { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
}

public class AdsOrderInfo
{
    public int Id { get; set; }
    public string PackageName { get; set; } = null!;
    public decimal? PricePaid { get; set; }
    public string Status { get; set; } = null!; 
    public DateTime CreatedAt { get; set; }
    public bool HasRefund { get; set; }
    public RefundInfo? RefundInfo { get; set; }
}

public class RefundInfo
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime RefundedAt { get; set; }
}
