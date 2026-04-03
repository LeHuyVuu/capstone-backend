using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services;

public class ReportTypeService : IReportTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReportTypeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<ReportTypeResponse>> GetReportTypesAsync(int page, int pageSize, bool? isActive = null)
    {
        var (reportTypes, totalCount) = await _unitOfWork.ReportTypes.GetPagedAsync(
            page,
            pageSize,
            filter: rt => rt.IsDeleted != true && (!isActive.HasValue || rt.IsActive == isActive.Value) && rt.TypeName == "FLAG",
            orderBy: q => q.OrderBy(rt => rt.TypeName)
        );

        var responses = _mapper.Map<List<ReportTypeResponse>>(reportTypes);
        return new PagedResult<ReportTypeResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<ReportTypeResponse?> GetReportTypeByIdAsync(int id)
    {
        var reportType = await _unitOfWork.ReportTypes.GetByIdAsync(id);

        if (reportType == null || reportType.IsDeleted == true)
            return null;

        return _mapper.Map<ReportTypeResponse>(reportType);
    }

    public async Task<ReportTypeResponse> CreateReportTypeAsync(CreateReportTypeRequest request)
    {
        var reportType = new ReportType
        {
            TypeName = request.TypeName,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.ReportTypes.AddAsync(reportType);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReportTypeResponse>(reportType);
    }

    public async Task<ReportTypeResponse?> UpdateReportTypeAsync(int id, UpdateReportTypeRequest request)
    {
        var reportType = await _unitOfWork.ReportTypes.GetByIdAsync(id);

        if (reportType == null || reportType.IsDeleted == true)
            return null;

        if (!string.IsNullOrWhiteSpace(request.TypeName))
            reportType.TypeName = request.TypeName;

        if (request.Description != null)
            reportType.Description = request.Description;

        if (request.IsActive.HasValue)
            reportType.IsActive = request.IsActive.Value;

        reportType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ReportTypes.Update(reportType);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReportTypeResponse>(reportType);
    }

    public async Task<bool> DeleteReportTypeAsync(int id)
    {
        var reportType = await _unitOfWork.ReportTypes.GetByIdAsync(id);

        if (reportType == null || reportType.IsDeleted == true)
            return false;

        reportType.IsDeleted = true;
        reportType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ReportTypes.Update(reportType);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
