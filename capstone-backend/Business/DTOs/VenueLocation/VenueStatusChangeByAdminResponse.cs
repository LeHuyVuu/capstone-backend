namespace capstone_backend.Business.DTOs.VenueLocation;

public class VenueStatusChangeByAdminResponse
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public int VenueOwnerId { get; set; }
    public string? VenueOwnerName { get; set; }
    public string PreviousStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AffectedAdvertisements { get; set; }
    public bool ReindexedInMeilisearch { get; set; }
}
