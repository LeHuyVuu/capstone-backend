namespace capstone_backend.Business.DTOs.Advertisement;

public class AdvertisementPackageResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int? PriorityScore { get; set; }
    public string? Placement { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
