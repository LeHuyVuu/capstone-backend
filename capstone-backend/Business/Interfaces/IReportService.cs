using capstone_backend.Business.DTOs.Report;

namespace capstone_backend.Business.Interfaces;

public interface IReportService
{
    Task<(IEnumerable<ReportDto> Reports, int TotalCount)> GetReportsAsync(GetReportsRequest request);
    Task<ReportDto?> GetReportByIdAsync(int id);
    Task<bool> ApproveReportAsync(int id);
    Task<bool> RejectReportAsync(int id);
}
