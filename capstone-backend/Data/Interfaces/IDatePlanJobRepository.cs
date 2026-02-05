using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IDatePlanJobRepository : IGenericRepository<DatePlanJob>
    {
        Task<DatePlanJob?> GetByDatePlanIdAndJobTypeAsync(int datePlanId, string jobType);
        Task<IEnumerable<DatePlanJob>> GetAllByDatePlanIdAsync(int datePlanId);
    }
}
