
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IAdminVoucherService
    {
        Task<PagedResult<AdminVoucherDetailResponse>> GetAdminVouchersAsync(GetAdminVouchersRequest query);
    }
}
