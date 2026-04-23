using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Report;

public class CreateVenueOwnerReviewReportRequest
{
    [Required(ErrorMessage = "ReportTypeId là bắt buộc")]
    public int ReportTypeId { get; set; }

    [StringLength(500, ErrorMessage = "Reason không được vượt quá 500 ký tự")]
    public string? Reason { get; set; } = string.Empty;
}