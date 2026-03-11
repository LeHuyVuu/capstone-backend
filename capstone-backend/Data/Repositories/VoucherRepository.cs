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

        public async Task<Voucher?> GetIncludeByIdAsync(int voucherId)
        {
            return await _dbSet
                .Include(v => v.VoucherLocations)
                .FirstOrDefaultAsync(v => v.Id == voucherId && v.IsDeleted == false);
        }

        public async Task<bool> IsDuplicateCodeAsync(string code)
        {
            return await _dbSet
                .AnyAsync(v => v.Code.ToLower() == code.ToLower() && v.IsDeleted == false);
        }
    }
}
