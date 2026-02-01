using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IDatePlanItemRepository : IGenericRepository<DatePlanItem>
    {
        Task<DatePlanItem?> GetByIdAndDatePlanIdAsync(int datePlanItemId, int datePlanId, bool includeItems = false);
        Task<IEnumerable<DatePlanItem>> GetByDatePlanIdAsync(int datePlanId);
    }
}
