using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SpecialEvent;

public class CreateSpecialEventRequest
{
    [Required(ErrorMessage = "Event name is required")]
    [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
    public string EventName { get; set; } = null!;
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}
