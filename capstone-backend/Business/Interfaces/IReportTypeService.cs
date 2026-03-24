using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Report;

namespace capstone_backend.Business.Interfaces;

public interface IReportTypeService
{
    Task<PagedResult<ReportTypeResponse>> GetReportTypesAsync(int page, int pageSize, bool? isActive = null);
    Task<ReportTypeResponse?> GetReportTypeByIdAsync(int id);
    Task<ReportTypeResponse> CreateReportTypeAsync(CreateReportTypeRequest request);
    Task<ReportTypeResponse?> UpdateReportTypeAsync(int id, UpdateReportTypeRequest request);
    Task<bool> DeleteReportTypeAsync(int id);
}
