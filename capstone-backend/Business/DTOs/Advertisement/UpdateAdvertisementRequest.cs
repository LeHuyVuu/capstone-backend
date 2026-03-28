using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class UpdateAdvertisementRequest
{
    [Required]
    public string Title { get; set; } = null!;
    
    public string? Content { get; set; }
    
    [Required]
    public string BannerUrl { get; set; } = null!;
    
    public string? TargetUrl { get; set; }
    
    [Required]
    public string PlacementType { get; set; } = null!;

    [Required]
    public int MoodTypeId { get; set; }
    
    [Required]
    public DateTime DesiredStartDate { get; set; }
}
