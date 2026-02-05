using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IDatePlanJobRepository : IGenericRepository<DatePlanJob>
    {
        Task<DatePlanJob?> GetByDatePlanIdAndJobType(int datePlanId, string jobType);
    }
}
