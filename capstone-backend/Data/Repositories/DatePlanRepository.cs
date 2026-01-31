using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanRepository : GenericRepository<DatePlan>, IDatePlanRepository
    {
        public DatePlanRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<DatePlan?> GetByIdAndCoupleIdAsync(int id, int coupleId)
        {
            return await _dbSet
                .Where(dp => dp.Id == id &&
                       dp.CoupleId == coupleId &&
                       dp.IsDeleted == false
                )
                .FirstOrDefaultAsync();
        }
    }
}
