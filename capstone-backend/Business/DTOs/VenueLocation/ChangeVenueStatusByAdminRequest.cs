using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.VenueLocation;

public class ChangeVenueStatusByAdminRequest
{
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = null!;
    
    public string? Reason { get; set; }
}
