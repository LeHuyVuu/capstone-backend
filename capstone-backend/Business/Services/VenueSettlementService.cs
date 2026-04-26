using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueSettlement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace capstone_backend.Business.Services
{
    public class VenueSettlementService : IVenueSettlementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public VenueSettlementService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RevenueResponse> GetRevenueAsync(int userId, RevenueRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var query = _unitOfWork.VenueSettlements.GetByVenueOwnerId(venueOwner.Id)
                .Where(x => x.IsDeleted == false &&
                            x.Status == VenueSettlementStatus.PAID.ToString() &&
                            x.PaidAt != null);

            DateTime? fromUtc = null;
            DateTime? toUtc = null;

            if (request.FromDate.HasValue)
                fromUtc = DateTimeNormalizeUtil.NormalizeToUtc(request.FromDate.Value);

            if (request.ToDate.HasValue)
            {
                var endOfDay = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                toUtc = DateTimeNormalizeUtil.NormalizeToUtc(endOfDay);
            }

            if (fromUtc.HasValue)
                query = query.Where(x => x.PaidAt >= fromUtc.Value);

            if (toUtc.HasValue)
                query = query.Where(x => x.PaidAt <= toUtc.Value);

            var groupBy = ResolveGroupBy(fromUtc, toUtc, request.GroupBy);

            var data = query.AsEnumerable();

            List<RevenueItem> result;

            if (groupBy == "day")
            {
                result = data
                    .GroupBy(x => TimezoneUtil.ToVietNamTime(x.PaidAt!.Value).Date)
                    .Select(g => new RevenueItem
                    {
                        Label = g.Key.ToString("yyyy-MM-dd"),
                        Revenue = g.Sum(x => x.NetAmount),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList();
            }
            else if (groupBy == "year")
            {
                result = data
                    .GroupBy(x => TimezoneUtil.ToVietNamTime(x.PaidAt!.Value).Year)
                    .Select(g => new RevenueItem
                    {
                        Label = g.Key.ToString(),
                        Revenue = g.Sum(x => x.NetAmount),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList();
            }
            else
            {
                result = data
                    .GroupBy(x =>
                    {
                        var local = TimezoneUtil.ToVietNamTime(x.PaidAt!.Value);
                        return new { local.Year, local.Month };
                    })
                    .Select(g => new RevenueItem
                    {
                        Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Revenue = g.Sum(x => x.NetAmount),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Label)
                    .ToList();
            }

            return new RevenueResponse
            {
                Items = result
            };
        }

        private string ResolveGroupBy(DateTime? fromUtc, DateTime? toUtc, string? requested)
        {

            if (!fromUtc.HasValue || !toUtc.HasValue)
                return requested?.Trim().ToLower() ?? "month";

            var days = (toUtc.Value - fromUtc.Value).TotalDays;

            if (days <= 31) return "day";
            if (days <= 365) return "month";
            return "year";
        }

        public async Task<VenueSettlementDetailResponse> GetSettlementDetailAsync(int userId, int settlementId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var settlement = await _unitOfWork.VenueSettlements.GetFirstAsync(
                x => x.Id == settlementId && x.IsDeleted == false && x.VenueOwnerId == venueOwner.Id,
                q => q
                    .Include(x => x.VoucherItem)
                        .ThenInclude(vi => vi.Voucher)
                    .Include(x => x.VoucherItem)
                    .Include(x => x.VoucherItemMember)
                        .ThenInclude(x => x.Member)
            );

            if (settlement == null)
                throw new Exception("Không tìm thấy thông tin quyết toán");

            var response = _mapper.Map<VenueSettlementDetailResponse>(settlement);
            return response;
        }

        public async Task<PagedResult<VenueSettlementListItemResponse>> GetSettlementsAsync(int userId, GetVenueSettlementsRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            // Create order ef
            Func<IQueryable<VenueSettlement>, IOrderedQueryable<VenueSettlement>> orderBy = q =>
                q.OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                var sortBy = request.SortBy.Trim().ToLower();
                var order = request.OrderBy?.Trim().ToLower() ?? "desc";

                orderBy = (sortBy, order) switch
                {
                    ("createdat", "asc") => q => q.OrderBy(x => x.CreatedAt),
                    ("createdat", "desc") => q => q.OrderByDescending(x => x.CreatedAt),
                    ("updatedat", "asc") => q => q.OrderBy(x => x.UpdatedAt),
                    ("updatedat", "desc") => q => q.OrderByDescending(x => x.UpdatedAt),
                    _ => q => q.OrderByDescending(x => x.CreatedAt)
                };
            }

            var (selltements, totalCount) = await _unitOfWork.VenueSettlements.GetPagedAsync(
                pageNumber,
                pageSize,
                vs => vs.IsDeleted == false &&
                    vs.VenueOwnerId == venueOwner.Id &&
                    (request.Status == null || vs.Status == request.Status.ToString()) &&
                    (!request.FromDate.HasValue || vs.CreatedAt >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || vs.CreatedAt <= request.ToDate.Value),
                orderBy,
                vs => vs.Include(x => x.VoucherItem)
                            .ThenInclude(x => x.Voucher)
            );

            var response = _mapper.Map<List<VenueSettlementListItemResponse>>(selltements);

            return new PagedResult<VenueSettlementListItemResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<VenueSettlementSummaryResponse> GetSummaryAsync(int userId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var query = _unitOfWork.VenueSettlements.GetByVenueOwnerId(venueOwner.Id);

            var pendingAmount = await query
                .Where(x => x.Status == VenueSettlementStatus.PENDING.ToString())
                .SumAsync(x => (decimal?)x.NetAmount) ?? 0;

            var paidAmount = await query
                .Where(x => x.Status == VenueSettlementStatus.PAID.ToString())
                .SumAsync(x => (decimal?)x.NetAmount) ?? 0;

            var cancelledAmount = await query
                .Where(x => x.Status == VenueSettlementStatus.CANCELLED.ToString())
                .SumAsync(x => (decimal?)x.NetAmount) ?? 0;

            var pendingCount = await query
                .CountAsync(x => x.Status == VenueSettlementStatus.PENDING.ToString());

            var paidCount = await query
                .CountAsync(x => x.Status == VenueSettlementStatus.PAID.ToString());

            var cancelledCount = await query
                .CountAsync(x => x.Status == VenueSettlementStatus.CANCELLED.ToString());

            return new VenueSettlementSummaryResponse
            {
                PendingAmount = pendingAmount,
                PaidAmount = paidAmount,
                CancelledAmount = cancelledAmount,
                PendingCount = pendingCount,
                PaidCount = paidCount,
                CancelledCount = cancelledCount
            };
        }
    }
}
