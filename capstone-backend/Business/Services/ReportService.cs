using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
                           .Include(r => r.ReportType)
        );

        var reportDtos = reports.Select(r => new ReportDto
        {
            Id = r.Id,
            ReporterId = r.ReporterId,
            ReporterName = r.Reporter?.FullName,
            ReportTypeId = r.ReportTypeId,
            ReportTypeName = r.ReportType?.TypeName,
            TargetType = ParseReportTargetType(r.TargetType),
            TargetId = r.TargetId,
            EvidenceSnapshot = ParseEvidenceSnapshot(r.EvidenceSnapshot),
            Reason = r.Reason,
            Status = ParseReportStatus(r.Status),
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
                           .Include(r => r.ReportType)
        );

        if (report == null)
            return null;

        return new ReportDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = report.Reporter?.FullName,
            ReportTypeId = report.ReportTypeId,
            ReportTypeName = report.ReportType?.TypeName,
            TargetType = ParseReportTargetType(report.TargetType),
            TargetId = report.TargetId,
            EvidenceSnapshot = ParseEvidenceSnapshot(report.EvidenceSnapshot),
            Reason = report.Reason,
            Status = ParseReportStatus(report.Status),
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<bool> ApproveReportAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetFirstAsync(
            predicate: r => r.Id == id && r.IsDeleted != true,
            include: r => r.Include(r => r.ReportType)
        );

        if (report == null)
            return false;

        if (report.TargetId.HasValue &&
            Enum.TryParse<ReportTargetType>(report.TargetType, ignoreCase: true, out var targetType))
        {
            var targetId = report.TargetId.Value;

            switch (targetType)
            {
                case ReportTargetType.POST:
                    var post = await _unitOfWork.Posts.GetByIdAsync(targetId);
                    if (post != null && post.IsDeleted != true)
                    {
                        post.Status = PostStatus.CANCELLED.ToString();
                        post.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Posts.Update(post);
                    }
                    break;

                case ReportTargetType.REVIEW:
                    var review = await _unitOfWork.Reviews.GetByIdAsync(targetId);
                    if (review != null && review.IsDeleted != true)
                    {
                        review.Status = ReviewStatus.CANCELLED.ToString();
                        review.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Reviews.Update(review);
                    }
                    break;

                case ReportTargetType.VOUCHER_ITEM:
                    if (report.ReportType != null &&
                        string.Equals(report.ReportType.TypeName, "VOUCHER_DISPUTE", StringComparison.OrdinalIgnoreCase))
                    {
                        var settlement = await _unitOfWork.VenueSettlements
                            .GetFirstAsync(s =>
                                s.VoucherItemId == targetId &&
                                s.IsDeleted != true);

                        if (settlement != null &&
                            string.Equals(settlement.Status, VenueSettlementStatus.DISPUTED.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            settlement.Status = VenueSettlementStatus.CANCELLED.ToString();
                            settlement.UpdatedAt = DateTime.UtcNow;

                            _unitOfWork.VenueSettlements.Update(settlement);
                        }
                    }
                    break;
            }
        }

        report.Status = ReportStatus.APPROVED.ToString();
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectReportAsync(int id)
    {
        var report = await _unitOfWork.Reports.GetFirstAsync(
            predicate: r => r.Id == id && r.IsDeleted != true && r.Status != ReportStatus.REJECTED.ToString(),
            include: r => r.Include(r => r.ReportType)
        );

        if (report == null)
            return false;

        if (report.TargetId.HasValue &&
            report.ReportType != null &&
            string.Equals(report.ReportType.TypeName, "VOUCHER_DISPUTE", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<ReportTargetType>(report.TargetType, true, out var targetType) &&
            targetType == ReportTargetType.VOUCHER_ITEM)
        {
            var settlement = await _unitOfWork.Context.VenueSettlements
                .FirstOrDefaultAsync(s =>
                    s.VoucherItemId == report.TargetId.Value &&
                    s.IsDeleted != true);

            if (settlement != null &&
                string.Equals(settlement.Status, VenueSettlementStatus.DISPUTED.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                settlement.Status = VenueSettlementStatus.PENDING.ToString();
                settlement.UpdatedAt = DateTime.UtcNow;
            }
        }



        report.Status = ReportStatus.REJECTED.ToString();
        report.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<ReportDto> CreateReportAsync(CreateReportRequest request, int reporterId)
    {
        var reporterProfile = await _unitOfWork.MembersProfile.GetFirstAsync(
            predicate: m => m.UserId == reporterId && m.IsDeleted != true
        );

        if (reporterProfile == null)
            throw new InvalidOperationException("Không tìm thấy hồ sơ thành viên cho tài khoản hiện tại");

        var reportType = await _unitOfWork.Context.ReportTypes
            .FirstOrDefaultAsync(rt => rt.Id == request.ReportTypeId && rt.IsDeleted != true && rt.IsActive == true);

        if (reportType == null)
            throw new InvalidOperationException($"Report type không hợp lệ: {request.ReportTypeId}");

        if (string.Equals(reportType.TypeName, "VOUCHER_DISPUTE", StringComparison.OrdinalIgnoreCase))
        {
            await HandleVoucherReportAsync(request, reporterProfile, reportType.TypeName);
        }

        var evidenceSnapshot = await BuildEvidenceSnapshotAsync(request.TargetType, request.TargetId);

        var report = new Data.Entities.Report
        {
            ReporterId = reporterProfile.Id,
            ReportTypeId = request.ReportTypeId,
            TargetType = request.TargetType.ToString(),
            TargetId = request.TargetId,
            EvidenceSnapshot = evidenceSnapshot,
            Reason = request.Reason,
            Status = ReportStatus.PENDING.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Reports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return new ReportDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = reporterProfile.FullName,
            ReportTypeId = report.ReportTypeId,
            ReportTypeName = reportType.TypeName,
            TargetType = request.TargetType,
            TargetId = report.TargetId,
            EvidenceSnapshot = ParseEvidenceSnapshot(report.EvidenceSnapshot),
            Reason = report.Reason,
            Status = ReportStatus.PENDING,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<ReportDto> CreateVenueOwnerReviewReportAsync(
        int reviewId,
        CreateVenueOwnerReviewReportRequest request,
        int venueOwnerUserId)
    {
        var venueOwnerProfile = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(venueOwnerUserId);
        if (venueOwnerProfile == null)
            throw new UnauthorizedAccessException("Không tìm thấy hồ sơ chủ địa điểm");

        var review = await _unitOfWork.Context.Reviews
            .Include(r => r.Venue)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.IsDeleted != true);

        if (review == null)
            throw new InvalidOperationException("Review không tồn tại");

        if (review.Venue == null || review.Venue.IsDeleted == true || review.Venue.VenueOwnerId != venueOwnerProfile.Id)
            throw new UnauthorizedAccessException("Bạn chỉ có thể tố cáo review thuộc địa điểm của mình");

        var reportType = await _unitOfWork.Context.ReportTypes
            .FirstOrDefaultAsync(rt => rt.Id == request.ReportTypeId && rt.IsDeleted != true && rt.IsActive == true);

        if (reportType == null)
            throw new InvalidOperationException($"Report type không hợp lệ: {request.ReportTypeId}");

        if (string.Equals(reportType.TypeName, "VOUCHER_DISPUTE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Loại report này không áp dụng cho review");

        var hasPendingReport = await _unitOfWork.Context.Reports.AnyAsync(r =>
            r.IsDeleted != true &&
            r.TargetType == ReportTargetType.REVIEW.ToString() &&
            r.TargetId == reviewId &&
            r.ReportTypeId == request.ReportTypeId &&
            r.Status == ReportStatus.PENDING.ToString());

        if (hasPendingReport)
            throw new InvalidOperationException("Review này đang có tố cáo cùng loại đang được xử lý");

        var reporterProfile = await _unitOfWork.MembersProfile.GetFirstAsync(
            predicate: m => m.UserId == venueOwnerUserId && m.IsDeleted != true);

        var evidenceSnapshot = await BuildEvidenceSnapshotAsync(ReportTargetType.REVIEW, reviewId);

        var report = new Data.Entities.Report
        {
            ReporterId = venueOwnerProfile.Id,
            ReportTypeId = request.ReportTypeId,
            TargetType = ReportTargetType.REVIEW.ToString(),
            TargetId = reviewId,
            EvidenceSnapshot = evidenceSnapshot,
            Reason = request.Reason?.Trim(),
            Status = ReportStatus.PENDING.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Reports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync();

        return new ReportDto
        {
            Id = report.Id,
            ReporterId = venueOwnerProfile.Id,
            ReporterName = reporterProfile?.FullName ?? venueOwnerProfile.BusinessName ?? $"Venue Owner #{venueOwnerProfile.Id}",
            ReportTypeId = report.ReportTypeId,
            ReportTypeName = reportType.TypeName,
            TargetType = ReportTargetType.REVIEW,
            TargetId = report.TargetId,
            EvidenceSnapshot = ParseEvidenceSnapshot(report.EvidenceSnapshot),
            Reason = report.Reason,
            Status = ReportStatus.PENDING,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private async Task HandleVoucherReportAsync(
        CreateReportRequest request,
        MemberProfile reporterProfile,
        string reportTypeName)
    {
        if (request.TargetType != ReportTargetType.VOUCHER_ITEM)
            throw new InvalidOperationException("Loại report VOUCHER_DISPUTE chỉ áp dụng cho voucher item");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do khiếu nại voucher");

        var voucherItem = await _unitOfWork.Context.VoucherItems
            .Include(v => v.VoucherItemMember)
                .ThenInclude(vim => vim.Member)
            .Include(v => v.Voucher)
            .FirstOrDefaultAsync(v => v.Id == request.TargetId && v.IsDeleted != true);

        if (voucherItem == null)
            throw new InvalidOperationException("Voucher item không tồn tại");

        if (voucherItem.VoucherItemMember == null || voucherItem.VoucherItemMember.MemberId != reporterProfile.Id)
            throw new InvalidOperationException("Bạn không thể khiếu nại voucher không thuộc về mình");

        if (!string.Equals(voucherItem.Status, VoucherItemStatus.USED.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể khiếu nại voucher đã sử dụng");

        if (!voucherItem.UsedAt.HasValue)
            throw new InvalidOperationException("Không xác định được thời điểm sử dụng voucher");

        if (DateTime.UtcNow > voucherItem.UsedAt.Value.AddDays(3))
            throw new InvalidOperationException("Đã quá thời hạn khiếu nại 3 ngày");

        var hasPendingReport = await _unitOfWork.Context.Reports
            .Include(r => r.ReportType)
            .AnyAsync(r =>
                r.IsDeleted != true &&
                r.TargetType == ReportTargetType.VOUCHER_ITEM.ToString() &&
                r.TargetId == request.TargetId &&
                r.Status == ReportStatus.PENDING.ToString() &&
                r.ReportType != null &&
                r.ReportType.TypeName == reportTypeName);

        if (hasPendingReport)
            throw new InvalidOperationException("Voucher này đang có khiếu nại đang được xử lý");

        var settlement = await _unitOfWork.Context.VenueSettlements
            .FirstOrDefaultAsync(s =>
                s.VoucherItemId == request.TargetId &&
                s.IsDeleted != true);

        if (settlement == null)
            throw new InvalidOperationException("Không tìm thấy bản ghi đối soát cho voucher này");

        if (string.Equals(settlement.Status, VenueSettlementStatus.DISPUTED.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Voucher này đang trong quá trình khiếu nại");

        if (string.Equals(settlement.Status, VenueSettlementStatus.PAID.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Voucher này đã được đối soát thanh toán, không thể khiếu nại");

        if (string.Equals(settlement.Status, VenueSettlementStatus.CANCELLED.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Voucher này đã bị hủy đối soát");

        settlement.Status = VenueSettlementStatus.DISPUTED.ToString();
        settlement.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<IEnumerable<ReportTypeDto>> GetAllReportTypesAsync()
    {
        var reportTypes = await _unitOfWork.Context.ReportTypes
            .Where(rt => rt.IsDeleted != true && rt.IsActive == true)
            .OrderBy(rt => rt.Id)
            .Select(rt => new ReportTypeDto
            {
                Id = rt.Id,
                TypeName = rt.TypeName,
                Description = rt.Description
            })
            .ToListAsync();

        return reportTypes;
    }

    private async Task<string> BuildEvidenceSnapshotAsync(ReportTargetType targetType, int targetId)
    {
        var capturedAt = DateTime.UtcNow;

        object? targetSnapshot = targetType switch
        {
            ReportTargetType.POST => await BuildPostSnapshotAsync(targetId),
            ReportTargetType.COMMENT => await BuildCommentSnapshotAsync(targetId),
            ReportTargetType.REVIEW => await BuildReviewSnapshotAsync(targetId),
            ReportTargetType.USER => await BuildUserSnapshotAsync(targetId),
            ReportTargetType.VENUE => await BuildVenueSnapshotAsync(targetId),
            ReportTargetType.VOUCHER_ITEM => await BuildVoucherItemSnapshotAsync(targetId),
            _ => null
        };

        if (targetSnapshot == null)
            throw new Exception($"Không tìm thấy target với TargetType={targetType}, TargetId={targetId}");

        var envelope = new
        {
            targetType = targetType.ToString(),
            targetId,
            capturedAt,
            data = targetSnapshot
        };

        return JsonSerializer.Serialize(envelope);
    }

    private async Task<object?> BuildVoucherItemSnapshotAsync(int targetId)
    {
        var voucherItem = await _unitOfWork.Context.VoucherItems
            .Include(v => v.Voucher)
            .Include(v => v.VoucherItemMember)
                .ThenInclude(vim => vim.Member)
            .FirstOrDefaultAsync(v => v.Id == targetId && v.IsDeleted != true);

        if (voucherItem == null)
            return null;

        return new
        {
            voucherItem.Id,
            voucherItem.VoucherId,
            voucherItem.VoucherItemMemberId,
            voucherItem.ItemCode,
            voucherItem.Status,
            voucherItem.AcquiredAt,
            voucherItem.UsedAt,
            voucherItem.ExpiredAt,
            VoucherTitle = voucherItem.Voucher?.Title,
            VoucherStatus = voucherItem.Voucher?.Status,
            MemberId = voucherItem.VoucherItemMember?.MemberId,
            MemberName = voucherItem.VoucherItemMember?.Member?.FullName
        };
    }

    private async Task<object?> BuildPostSnapshotAsync(int targetId)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(targetId);
        if (post == null || post.IsDeleted == true)
            return null;

        return new
        {
            post.Id,
            post.AuthorId,
            post.Content,
            post.MediaPayload,
            post.HashTags,
            post.Status,
            post.CreatedAt,
            post.UpdatedAt
        };
    }

    private async Task<object?> BuildCommentSnapshotAsync(int targetId)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(targetId);
        if (comment == null || comment.IsDeleted == true)
            return null;

        return new
        {
            comment.Id,
            comment.PostId,
            comment.AuthorId,
            comment.TargetMemberId,
            comment.Content,
            comment.ParentId,
            comment.RootId,
            comment.Status,
            comment.CreatedAt,
            comment.UpdatedAt
        };
    }

    private async Task<object?> BuildReviewSnapshotAsync(int targetId)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(targetId);
        if (review == null || review.IsDeleted == true)
            return null;

        return new
        {
            review.Id,
            review.VenueId,
            review.MemberId,
            review.Rating,
            review.Content,
            review.VisitedAt,
            review.ImageUrls,
            review.Status,
            review.CreatedAt,
            review.UpdatedAt
        };
    }

    private async Task<object?> BuildUserSnapshotAsync(int targetId)
    {
        var user = await _unitOfWork.MembersProfile.GetByIdAsync(targetId);
        if (user == null || user.IsDeleted == true)
            return null;

        return new
        {
            user.Id,
            user.UserId,
            user.FullName,
            user.Gender,
            user.Bio,
            user.RelationshipStatus,
            user.CreatedAt,
            user.UpdatedAt
        };
    }

    private async Task<object?> BuildVenueSnapshotAsync(int targetId)
    {
        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(targetId);
        if (venue == null || venue.IsDeleted == true)
            return null;

        return new
        {
            venue.Id,
            venue.VenueOwnerId,
            venue.Name,
            venue.Description,
            venue.Address,
            venue.PhoneNumber,
            venue.Email,
            venue.Category,
            venue.Status,
            venue.CoverImage,
            venue.CreatedAt,
            venue.UpdatedAt
        };
    }

    private static ReportTargetType? ParseReportTargetType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<ReportTargetType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static ReportStatus? ParseReportStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<ReportStatus>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static object? ParseEvidenceSnapshot(string? evidenceSnapshot)
    {
        if (string.IsNullOrWhiteSpace(evidenceSnapshot))
            return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(evidenceSnapshot);
        }
        catch
        {
            return evidenceSnapshot;
        }
    }
}
