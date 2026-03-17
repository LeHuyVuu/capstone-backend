namespace capstone_backend.Business.DTOs.SubscriptionPackage;

public class VenueSubscriptionPackageDto
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public int PackageId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Quantity { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Package details
    public SubscriptionPackageDto? Package { get; set; }
}
