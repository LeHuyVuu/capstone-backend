using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVoucherJobRepository : IGenericRepository<VoucherJob>
    {
        Task<VoucherJob?> GetByVoucherIdAndTypeAsync(int id, string type);
    }
}
