using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanRepository : GenericRepository<DatePlan>, IDatePlanRepository
    {
        public DatePlanRepository(MyDbContext context) : base(context)
        {
        }
    }
}
