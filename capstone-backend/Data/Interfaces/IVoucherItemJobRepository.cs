using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVoucherItemJobRepository : IGenericRepository<VoucherItemJob>
    {
        Task<VoucherItemJob?> GetByVoucherItemIdAndTypeAsync(int id, string type);
    }
}
