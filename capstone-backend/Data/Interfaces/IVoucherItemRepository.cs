using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVoucherItemRepository : IGenericRepository<VoucherItem>
    {
        Task<bool> IsExistedCodeAsync(string code);
        Task ExecuteUpdateUnassignedVoucherItemsAsync(int voucherId);
    }
}
