namespace capstone_backend.Business.DTOs.SubscriptionPackage;

public class SubscriptionPackageDto
{
    public int Id { get; set; }
    public string? PackageName { get; set; }
    public decimal? Price { get; set; }
    public int? DurationDays { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
