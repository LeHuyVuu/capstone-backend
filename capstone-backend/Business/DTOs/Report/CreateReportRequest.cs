using System.ComponentModel.DataAnnotations;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Report;

public class CreateReportRequest
{
    [Required(ErrorMessage = "TargetType là bắt buộc")]
    public ReportTargetType TargetType { get; set; }

    [Required(ErrorMessage = "TargetId là bắt buộc")]
    public int TargetId { get; set; }

    [Required(ErrorMessage = "Reason là bắt buộc")]
    [StringLength(500, ErrorMessage = "Reason không được vượt quá 500 ký tự")]
    public string Reason { get; set; } = string.Empty;
}
