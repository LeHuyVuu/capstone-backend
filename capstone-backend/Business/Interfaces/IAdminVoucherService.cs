
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IAdminVoucherService
    {
        Task<AdminVoucherDetailResponse> GetAdminVoucherByIdAsync(int voucherId);
        Task<PagedResult<AdminVoucherDetailResponse>> GetAdminVouchersAsync(GetAdminVouchersRequest query);
        Task<PagedResult<AdminVoucherDetailResponse>> GetPendingVouchersAsync(GetPendingVouchersRequest query);
    }
}
