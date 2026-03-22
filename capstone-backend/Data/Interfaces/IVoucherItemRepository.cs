using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVoucherItemRepository : IGenericRepository<VoucherItem>
    {
        Task<bool> IsExistedCodeAsync(string code);
        Task ExecuteUpdateUnassignedVoucherItemsAsync(int voucherId);
        Task<VoucherItem?> GetIncludeByIdAsync(int id);
        Task<VoucherItem?> GetByItemCodeWithDetailsAsync(string itemCode);
        Task<int> CountMemberAcquiredVoucherAsync(int memberId, int voucherId);
        Task<IEnumerable<VoucherItem>> GetAvailableVoucherItemsForExchangeAsync(int voucherId, int quantity);
        Task<Dictionary<int, int>> CountMemberAcquiredVouchersAsync(int memberId, List<int> voucherIds);
    }
}
