using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueSettlement;

namespace capstone_backend.Business.Interfaces
{
    public interface IVenueSettlementService
    {
        Task<VenueSettlementSummaryResponse> GetSummaryAsync(int userId);
        Task<PagedResult<VenueSettlementListItemResponse>> GetSettlementsAsync(int userId, GetVenueSettlementsRequest request);
        Task<VenueSettlementDetailResponse> GetSettlementDetailAsync(int userId, int settlementId);
    }
}
