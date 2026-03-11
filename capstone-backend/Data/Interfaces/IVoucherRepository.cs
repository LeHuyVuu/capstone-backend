using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVoucherRepository : IGenericRepository<Voucher>
    {
        Task<bool> IsDuplicateCodeAsync(string code);
        Task<Voucher?> GetIncludeByIdAsync(int voucherId);
    }
}
