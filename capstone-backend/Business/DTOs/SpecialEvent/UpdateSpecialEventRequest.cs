using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SpecialEvent;

public class UpdateSpecialEventRequest
{
    [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
    public string? EventName { get; set; }
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}
