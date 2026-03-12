using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VoucherJobRepository : GenericRepository<VoucherJob>, IVoucherJobRepository
    {
        public VoucherJobRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<VoucherJob?> GetByVoucherIdAndTypeAsync(int id, string type)
        {
            return await _dbSet
                .FirstOrDefaultAsync(vj => vj.VoucherId == id && vj.JobType == type);
        }
    }
}
