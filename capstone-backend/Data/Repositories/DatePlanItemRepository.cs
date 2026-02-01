using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanItemRepository : GenericRepository<DatePlanItem>, IDatePlanItemRepository
    {
        public DatePlanItemRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DatePlanItem>> GetByDatePlanIdAsync(int datePlanId)
        {
            return await _dbSet
                .Where(dpi => dpi.DatePlanId == datePlanId &&
                       dpi.IsDeleted == false
                )
                .ToListAsync();
        }

        public async Task<DatePlanItem?> GetByIdAndDatePlanIdAsync(int datePlanItemId, int datePlanId, bool includeItems = false)
        {
            IQueryable<DatePlanItem> query = _dbSet
                .Where(dpi => dpi.Id == datePlanItemId &&
                       dpi.DatePlanId == datePlanId &&
                       dpi.IsDeleted == false
                );

            if (includeItems)
            {
                query = query
                    .Include(dpi => dpi.DatePlan);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}
