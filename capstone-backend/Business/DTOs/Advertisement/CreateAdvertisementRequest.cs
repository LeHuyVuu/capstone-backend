using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class CreateAdvertisementRequest
{
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
    public string? Content { get; set; }

    [Required(ErrorMessage = "BannerUrl là bắt buộc")]
    public string BannerUrl { get; set; } = null!;

    [Url(ErrorMessage = "TargetUrl phải là URL hợp lệ")]
    public string? TargetUrl { get; set; }

    [StringLength(50, ErrorMessage = "PlacementType không được vượt quá 50 ký tự")]
    public string? PlacementType { get; set; }

    [Required(ErrorMessage = "MoodTypeId là bắt buộc")]
    public int MoodTypeId { get; set; }

    /// <summary>
    /// Desired start date for the advertisement campaign
    /// If admin approves after this date, start date will be auto-adjusted to approval date
    /// </summary>
    [Required(ErrorMessage = "DesiredStartDate là bắt buộc")]
    public DateTime DesiredStartDate { get; set; }
}
