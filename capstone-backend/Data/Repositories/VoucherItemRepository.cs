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

        public async Task<int> CountMemberAcquiredVoucherAsync(int memberId, int voucherId)
        {
            return await _dbSet
                .Where(vi => vi.VoucherId == voucherId && vi.IsDeleted == false && vi.VoucherItemMember != null && vi.VoucherItemMember.MemberId == memberId)
                .CountAsync();
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

        public async Task<IEnumerable<VoucherItem>> GetAvailableVoucherItemsForExchangeAsync(int voucherId, int quantity)
        {
            return await _dbSet
                .Where(vi =>
                    vi.VoucherId == voucherId &&
                    vi.IsDeleted == false &&
                    vi.VoucherItemMemberId == null &&
                    vi.Status == VoucherItemStatus.AVAILABLE.ToString()
                ).OrderBy(vi => vi.Id)
                .Take(quantity)
                .ToListAsync();
        }

        public async Task<VoucherItem?> GetByItemCodeWithDetailsAsync(string itemCode)
        {
            return await _dbSet
                .Include(vi => vi.Voucher)
                    .ThenInclude(v => v.VoucherLocations)
                .Include(vi => vi.VoucherItemMember)
                    .ThenInclude(vim => vim.Member)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(vi => vi.ItemCode.ToLower() == itemCode.ToLower() && vi.IsDeleted == false);
        }

        public async Task<VoucherItem?> GetIncludeByIdAsync(int id)
        {
            return await _dbSet
                .Include(vi => vi.Voucher)
                .Include(vi => vi.VoucherItemMember)
                    .ThenInclude(vim => vim.Member)
                        .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(vi => vi.Id == id && vi.IsDeleted == false);
        }

        public async Task<bool> IsExistedCodeAsync(string code)
        {
            return await _dbSet
                .AnyAsync(vi => vi.ItemCode.ToLower() == code.ToLower() && vi.IsDeleted == false);
        }
    }
}
