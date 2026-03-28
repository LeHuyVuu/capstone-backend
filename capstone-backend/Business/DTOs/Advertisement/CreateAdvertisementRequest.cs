using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class CreateAdvertisementRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Content cannot exceed 1000 characters")]
    public string? Content { get; set; }

    [Required(ErrorMessage = "BannerUrl is required")]
    public string BannerUrl { get; set; } = null!;

    [Url(ErrorMessage = "TargetUrl must be a valid URL")]
    public string? TargetUrl { get; set; }

    [Required(ErrorMessage = "PlacementType is required")]
    [StringLength(50, ErrorMessage = "PlacementType cannot exceed 50 characters")]
    public string PlacementType { get; set; } = null!;

    [Required(ErrorMessage = "MoodTypeId is required")]
    public int MoodTypeId { get; set; }

    /// <summary>
    /// Desired start date for the advertisement campaign
    /// If admin approves after this date, start date will be auto-adjusted to approval date
    /// </summary>
    [Required(ErrorMessage = "DesiredStartDate is required")]
    public DateTime DesiredStartDate { get; set; }
}
