using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(IEnumerable<ReportDto> Reports, int TotalCount)> GetReportsAsync(GetReportsRequest request)
    {
        var (reports, totalCount) = await _unitOfWork.Reports.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            filter: r => r.IsDeleted != true &&
                        (!request.Status.HasValue || r.Status == request.Status.Value.ToString()) &&
                        (!request.TargetType.HasValue || r.TargetType == request.TargetType.Value.ToString()),
            orderBy: q => q.OrderByDescending(r => r.CreatedAt),
            include: q => q.Include(r => r.Reporter)
        );

        var reportDtos = reports.Select(r => new ReportDto
        {
            Id = r.Id,
            ReporterId = r.ReporterId,
            ReporterName = r.Reporter?.FullName,
            TargetType = string.IsNullOrEmpty(r.TargetType) ? null : Enum.Parse<ReportTargetType>(r.TargetType),
            TargetId = r.TargetId,
            Reason = r.Reason,
            Status = string.IsNullOrEmpty(r.Status) ? null : Enum.Parse<ReportStatus>(r.Status),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });

        return (reportDtos, totalCount);
    }

    public async Task<ReportDto?> GetReportByIdAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetFirstAsync(
            predicate: r => r.Id == id && r.IsDeleted != true,
            include: q => q.Include(r => r.Reporter)
        );

        if (report == null)
            return null;

        return new ReportDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = report.Reporter?.FullName,
            TargetType = string.IsNullOrEmpty(report.TargetType) ? null : Enum.Parse<ReportTargetType>(report.TargetType),
            TargetId = report.TargetId,
            Reason = report.Reason,
            Status = string.IsNullOrEmpty(report.Status) ? null : Enum.Parse<ReportStatus>(report.Status),
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<bool> ApproveReportAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetFirstAsync(
            predicate: r => r.Id == id && r.IsDeleted != true
        );

        if (report == null)
            return false;

        report.Status = ReportStatus.APPROVED.ToString();
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectReportAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetFirstAsync(
            predicate: r => r.Id == id && r.IsDeleted != true
        );

        if (report == null)
            return false;

        report.Status = ReportStatus.REJECTED.ToString();
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
