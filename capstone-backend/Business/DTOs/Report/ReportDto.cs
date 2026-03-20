using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Report;

public class ReportDto
{
    public int Id { get; set; }
    public int? ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public ReportTargetType? TargetType { get; set; }
    public int? TargetId { get; set; }
    public string? Reason { get; set; }
    public ReportStatus? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
