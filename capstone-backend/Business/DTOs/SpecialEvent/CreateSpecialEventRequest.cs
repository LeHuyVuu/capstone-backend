using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SpecialEvent;

public class CreateSpecialEventRequest
{
    [Required(ErrorMessage = "Event name is required")]
    [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
    public string EventName { get; set; } = null!;
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    [StringLength(500, ErrorMessage = "Banner URL cannot exceed 500 characters")]
    public string? BannerUrl { get; set; }
    
    /// <summary>
    /// Nếu true: sự kiện lặp lại hằng năm (chỉ lưu ngày/tháng, bỏ qua năm)
    /// Nếu false: sự kiện một lần với năm cụ thể
    /// </summary>
    public bool IsYearly { get; set; } = true;
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}
