using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VoucherItemJobRepository : GenericRepository<VoucherItemJob>, IVoucherItemJobRepository
    {
        public VoucherItemJobRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<VoucherItemJob?> GetByVoucherItemIdAndTypeAsync(int id, string type)
        {
            return await _dbSet
                .FirstOrDefaultAsync(vij => vij.VoucherItemId == id && vij.JobType == type);
        }
    }
}
