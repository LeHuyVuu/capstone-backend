namespace capstone_backend.Business.DTOs.Advertisement;

public class MyAdvertisementResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string BannerUrl { get; set; } = null!;
    public string PlacementType { get; set; } = null!;
    public string Status { get; set; } = null!; // DRAFT, PENDING, ACTIVE, REJECTED, EXPIRED
    public List<RejectionHistoryEntry>? RejectionHistory { get; set; }
    public DateTime? DesiredStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Number of venue locations using this ad
    public int VenueLocationCount { get; set; }
    
    // Active venue location ad (if any)
    public ActiveVenueLocationAd? ActiveVenueAd { get; set; }
}

public class ActiveVenueLocationAd
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? PriorityScore { get; set; }
}
