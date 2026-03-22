namespace capstone_backend.Business.DTOs.Advertisement;

public class AdsOrderResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Payment information
    public PaymentInfo? Payment { get; set; }
    
    // Package information
    public PackageInfo? Package { get; set; }
    
    // Advertisement information
    public AdvertisementInfo? Advertisement { get; set; }
    
    // Venue location advertisements
    public List<VenueLocationAdInfo>? VenueLocationAds { get; set; }
}

public class PaymentInfo
{
    public int? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = null!; // PENDING, COMPLETED, FAILED, REFUNDED
    public string PaymentMethod { get; set; } = null!; // MOMO, SEPAY, etc.
    public DateTime? PaidAt { get; set; }
    public string? TransactionCode { get; set; }
}

public class PackageInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public string PlacementType { get; set; } = null!;
}

public class ActivePeriod
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? PriorityScore { get; set; }
    public bool IsActive { get; set; }
}

public class VenueLocationInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? CoverImage { get; set; }
}

public class AdvertisementInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public string BannerUrl { get; set; } = null!;
    public string? TargetUrl { get; set; }
    public string PlacementType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? DesiredStartDate { get; set; }
}