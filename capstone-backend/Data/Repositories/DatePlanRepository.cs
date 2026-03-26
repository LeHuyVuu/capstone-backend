using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
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

        public async Task<DatePlan?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Where(dp => dp.Id == id && dp.IsDeleted == false)
                .FirstOrDefaultAsync();
        }

        public async Task<DatePlan?> GetByIdAndCoupleIdAsync(int id, int coupleId, bool includeItems = false, bool includeVenueLocation = false)
        {
            //return await _dbSet
            //    .Where(dp => dp.Id == id &&
            //           dp.CoupleId == coupleId &&
            //           dp.IsDeleted == false
            //    )
            //    .FirstOrDefaultAsync();
            IQueryable<DatePlan> query = _dbSet
                .Include(dp => dp.OrganizerMember)
                .Where(dp => dp.Id == id &&
                       dp.CoupleId == coupleId &&
                       dp.IsDeleted == false
                );

            if (includeItems)
            {
                query = query
                    .Include(dp => dp.DatePlanItems
                        .Where(dpi => dpi.IsDeleted == false)
                        .OrderBy(dpi => dpi.OrderIndex));
            }

            if (includeVenueLocation)
            {
                query = query
                    .Include(dp => dp.DatePlanItems)
                        .ThenInclude(dpi => dpi.VenueLocation);
            }

            return await query.FirstOrDefaultAsync();

        }

        public async Task<IEnumerable<DatePlan>> GetAllExpiredPlansAsync(DateTime thresholdTime)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(dp => dp.IsDeleted == false && dp.PlannedEndAt < thresholdTime)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingAsync(
            int coupleId,
            DateTime plannedStartAt,
            DateTime plannedEndAt,
            int? excludeDatePlanId = null)
        {
            return await _dbSet.AnyAsync(dp =>
                dp.CoupleId == coupleId &&
                dp.IsDeleted == false &&
                dp.PlannedStartAt.HasValue &&
                dp.PlannedEndAt.HasValue &&
                (!excludeDatePlanId.HasValue || dp.Id != excludeDatePlanId.Value) &&
                (
                    dp.Status == DatePlanStatus.DRAFTED.ToString() ||
                    dp.Status == DatePlanStatus.PENDING.ToString() ||
                    dp.Status == DatePlanStatus.SCHEDULED.ToString() ||
                    dp.Status == DatePlanStatus.IN_PROGRESS.ToString()
                ) &&
                plannedStartAt < dp.PlannedEndAt.Value &&
                plannedEndAt > dp.PlannedStartAt.Value
            );
        }
    }
}
