using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.Arm;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanRepository : GenericRepository<DatePlan>, IDatePlanRepository
    {
        public DatePlanRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<DatePlan?> GetByIdAndCoupleIdAsync(int id, int coupleId, bool includeItems = false)
        {
            //return await _dbSet
            //    .Where(dp => dp.Id == id &&
            //           dp.CoupleId == coupleId &&
            //           dp.IsDeleted == false
            //    )
            //    .FirstOrDefaultAsync();
            IQueryable<DatePlan> query = _dbSet
                .AsNoTracking()
                .Where(dp => dp.Id == id &&
                       dp.CoupleId == coupleId &&
                       dp.IsDeleted == false
                );

            if (includeItems)
            {
                query = query
                    .Include(dp => dp.DatePlanItems.Where(dpi => dpi.IsDeleted == false).OrderBy(dpi => dpi.OrderIndex));
            }

            return await query.FirstOrDefaultAsync();

        }
    }
}
