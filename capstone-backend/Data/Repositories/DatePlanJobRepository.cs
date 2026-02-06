using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanJobRepository : GenericRepository<DatePlanJob>, IDatePlanJobRepository
    {
        public DatePlanJobRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<DatePlanJob?> GetByDatePlanIdAndJobTypeAsync(int datePlanId, string jobType)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(dpj => dpj.DatePlanId == datePlanId && dpj.JobType == jobType)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<DatePlanJob>> GetAllByDatePlanIdAsync(int datePlanId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(dpj => dpj.DatePlanId == datePlanId)
                .ToListAsync();
        }
    }
}
