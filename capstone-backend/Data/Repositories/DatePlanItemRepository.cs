using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class DatePlanItemRepository : GenericRepository<DatePlanItem>, IDatePlanItemRepository
    {
        public DatePlanItemRepository(MyDbContext context) : base(context)
        {
        }
    }
}
