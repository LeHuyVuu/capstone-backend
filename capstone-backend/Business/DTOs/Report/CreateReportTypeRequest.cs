using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Report;

public class CreateReportTypeRequest
{
    [Required(ErrorMessage = "Type name is required")]
    [MaxLength(100, ErrorMessage = "Type name cannot exceed 100 characters")]
    public string TypeName { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
