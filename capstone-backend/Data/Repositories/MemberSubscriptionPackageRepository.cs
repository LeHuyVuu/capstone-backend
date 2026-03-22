using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class MemberSubscriptionPackageRepository : GenericRepository<MemberSubscriptionPackage>, IMemberSubscriptionPackageRepository
    {
        public MemberSubscriptionPackageRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<MemberSubscriptionPackage?> GetCurrentActiveSubscriptionAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(s => s.MemberId == id && s.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString());
        }
    }
}
