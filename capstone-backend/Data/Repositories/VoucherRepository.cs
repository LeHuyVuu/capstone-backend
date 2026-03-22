using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VoucherRepository : GenericRepository<Voucher>, IVoucherRepository
    {
        public VoucherRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Voucher>> GetByIdsWithItemsAsync(List<int> voucherIds)
        {
            return await _dbSet
                .Include(v => v.VoucherItems.Where(vi => vi.IsDeleted == false))
                .Where(v => voucherIds.Contains(v.Id) && v.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<IEnumerable<Voucher>> GetByVenueOwnerIdAsync(int venueOwnerId)
        {
            return await _dbSet
                .Where(v => v.VenueOwnerId == venueOwnerId && v.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Voucher?> GetIncludeByIdAsync(int voucherId)
        {
            return await _dbSet
                .Include(v => v.VenueOwner)
                .Include(v => v.VoucherLocations)
                    .ThenInclude(vl => vl.VenueLocation)
                .FirstOrDefaultAsync(v => v.Id == voucherId && v.IsDeleted == false);
        }

        public async Task<bool> IsDuplicateCodeAsync(string code)
        {
            return await _dbSet
                .AnyAsync(v => v.Code.ToLower() == code.ToLower() && v.IsDeleted == false);
        }
    }
}
