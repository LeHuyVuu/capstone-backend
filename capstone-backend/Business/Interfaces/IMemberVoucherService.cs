using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberVoucherService
    {
        Task<MemberVoucherDetailResponse> GetMemberVoucherByIdAsync(int voucherId);
        Task<PagedResult<MemberVoucherListItemResponse>> GetMemberVouchersAsync(GetMemberVouchersRequest request);
    }
}
