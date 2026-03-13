using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IVenueVoucherService
    {
        Task<PagedResult<VoucherDetailResponse>> GetVenueVouchersAsync(int userId, GetVenueVouchersRequest query);
        Task<VoucherResponse> CreateVenueVoucherAsync(int userId, CreateVoucherRequest request);
        Task<bool> DeleteVenueVoucherAsync(int userId, int voucherId);        
        Task<VoucherResponse> RevokeSubmittedVoucherAsync(int userId, int voucherId);
        Task<VoucherResponse> SubmitVoucherAsync(int userId, int voucherId);
        Task<VoucherResponse> UpdateVenueVoucherAsync(int userId, int voucherId, UpdateVoucherRequest request);
        Task<VoucherDetailResponse> GetVoucherByIdAsync(int userId, int voucherId);
        Task<VoucherSummaryResponse> GetVoucherSummaryByIdAsync(int userId, int voucherId);
        Task<int> ActivateVoucherAsync(int userId, int voucherId);
        Task<int> EndVoucherAsync(int userId, int voucherId);
        Task<PagedResult<VoucherItemResponse>> GetVoucherItemsByVoucherIdAsync(int userId, int voucherId, GetVoucherItemsRequest query);
        Task<VoucherItemDetailResponse> GetVoucherItemByIdAsync(int userId, int voucherItemId);
        Task<VoucherItemValidationAndRedemptionResponse> ValidateVoucherCodeAsync(int userId, ValidateAndRedeemVoucherItemRequest request);
        Task<VoucherItemValidationAndRedemptionResponse> RedeemVoucherCodeAsync(int userId, ValidateAndRedeemVoucherItemRequest request);
        Task<PagedResult<VenueVoucherActivityResponse>> GetVoucherRedemptionsAsync(int userId, int voucherId, GetVoucherActivityRequest query);
        Task<PagedResult<VenueVoucherActivityResponse>> GetVoucherExchangesAsync(int userId, int voucherId, GetVoucherActivityRequest query);
    }
}
