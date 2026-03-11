using capstone_backend.Business.DTOs.Voucher;

namespace capstone_backend.Business.Interfaces
{
    public interface IVenueVoucherService
    {
        Task<VoucherResponse> CreateVenueVoucherAsync(int userId, CreateVoucherRequest request);
        Task<bool> DeleteVenueVoucherAsync(int userId, int voucherId);
        Task<VoucherResponse> SubmitVoucherAsync(int userId, int voucherId);
        Task<VoucherResponse> UpdateVenueVoucherAsync(int userId, int voucherId, UpdateVoucherRequest request);
    }
}
