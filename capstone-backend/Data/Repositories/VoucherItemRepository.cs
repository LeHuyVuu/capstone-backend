using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VoucherItemRepository : GenericRepository<VoucherItem>, IVoucherItemRepository
    {
        public VoucherItemRepository(MyDbContext context) : base(context)
        {
        }

        public async Task ExecuteUpdateUnassignedVoucherItemsAsync(int voucherId)
        {
            await _dbSet
                .Where(vi => vi.VoucherId == voucherId && vi.IsDeleted == false && vi.Status == VoucherItemStatus.AVAILABLE.ToString())
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(vi => vi.Status, VoucherItemStatus.ENDED.ToString())
                    .SetProperty(vi => vi.UpdatedAt, DateTime.UtcNow)
                );
        }

        public async Task<bool> IsExistedCodeAsync(string code)
        {
            return await _dbSet
                .AnyAsync(vi => vi.ItemCode.ToLower() == code.ToLower() && vi.IsDeleted == false);
        }
    }
}
