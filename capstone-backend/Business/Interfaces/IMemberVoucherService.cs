using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberVoucherService
    {
        Task<ExchangeVoucherResponse> ExchangeVoucherAsync(int userId, ExchangeVoucherRequest request);
        Task<MemberVoucherDetailResponse> GetMemberVoucherByIdAsync(int voucherId);
        Task<PagedResult<MemberVoucherListItemResponse>> GetMemberVouchersAsync(GetMemberVouchersRequest request);
        Task<MemberVoucherTransactionDetailResponse> GetMemberVoucherTransactionDetailsAsync(int userId, int voucherItemMemberId);
        Task<PagedResult<MemberVoucherTransactionListItemResponse>> GetMemberVoucherTransactionsAsync(int userId, GetMemberVoucherTransactionsRequest request);
        Task<MemberVoucherItemDetailResponse> GetMyVoucherDetailsAsync(int userId, int voucherItemId);
        Task<PagedResult<MemberVoucherItemResponse>> GetMyVouchersAsync(int userId, GetMyVouchersRequest request);
    }
}
