using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Report;

public class UpdateReportTypeRequest
{
    [MaxLength(100, ErrorMessage = "Type name cannot exceed 100 characters")]
    public string? TypeName { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
