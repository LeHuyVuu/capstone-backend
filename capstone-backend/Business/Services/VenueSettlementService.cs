using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueSettlement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class VenueSettlementService : IVenueSettlementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VenueSettlementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<VenueSettlementDetailResponse> GetSettlementDetailAsync(int userId, int settlementId)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<VenueSettlementListItemResponse>> GetSettlementsAsync(int userId, GetVenueSettlementsRequest request)
        {
            throw new NotImplementedException();
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
