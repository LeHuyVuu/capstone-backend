using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueSettlement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
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

        public Task<VenueSettlementDetailResponse> GetSettlementDetailAsync(int userId, int settlementId)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<VenueSettlementListItemResponse>> GetSettlementsAsync(int userId, GetVenueSettlementsRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetIncludeByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy chủ địa điểm");

            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var status = request.Status.ToString().Trim().ToUpper();

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

            var vouchers = await _unitOfWork.Vouchers.GetByVenueOwnerIdAsync(venueOwner.Id);
            var voucherDict = vouchers.ToDictionary(v => v.Id, v => v);
            var voucherIds = vouchers.Select(v => v.Id).ToList();

            var voucherItems = await _unitOfWork.VoucherItems.GetByVoucherIdsAsync(voucherIds);
            var voucherItemDict = voucherItems.ToDictionary(vi => vi.Id, vi => vi);
            var voucherItemIds = voucherItems.Select(vi => vi.Id).ToList();

            var (selltements, totalCount) = await _unitOfWork.VenueSettlements.GetPagedAsync(
                pageNumber,
                pageSize,
                vs => vs.IsDeleted == false &&
                    voucherItemIds.Contains(vs.VoucherItemId) &&
                    (string.IsNullOrEmpty(status) || vs.Status == status) &&
                    (!request.FromDate.HasValue || vs.CreatedAt >= request.FromDate.Value) &&
                    (!request.ToDate.HasValue || vs.CreatedAt <= request.ToDate.Value),
                orderBy
            );

            var response = _mapper.Map<List<VenueSettlementListItemResponse>>(selltements);
            response = response.Select(r =>
            {
                var vItem = voucherItemDict.GetValueOrDefault(r.VoucherItemId);
                var voucher = vItem != null ? voucherDict.GetValueOrDefault(vItem.VoucherId) : null;

                r.VoucherItemCode = vItem?.ItemCode;
                r.VoucherTitle = voucher?.Title;

                return r;
            }).ToList();

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
