using AutoMapper.Execution;
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
            var now = DateTime.UtcNow;

            return await _dbSet
                 .Include(x => x.Package)
                 .FirstOrDefaultAsync(x =>
                     x.MemberId == id &&
                     x.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString() &&
                     (!x.EndDate.HasValue || x.EndDate >= now) &&
                     x.Package != null &&
                     x.Package.IsDeleted != true &&
                     x.Package.IsActive == true);
        }

        public async Task<IEnumerable<MemberSubscriptionPackage>> GetExpiredSubscriptionsAsync(DateTime now)
        {
            return await _dbSet
                .Where(msp => msp.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString()
                           && msp.EndDate != null
                           && msp.EndDate <= now
                )
                .OrderBy(msp => msp.EndDate)
                .ToListAsync();
        }
    }
}
