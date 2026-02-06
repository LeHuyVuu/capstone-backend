using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IDatePlanRepository : IGenericRepository<DatePlan>
    {
        Task<DatePlan?> GetByIdAndCoupleIdAsync(int id, int coupleId, bool includeItems = false, bool includeVenueLocation = false);
        Task<IEnumerable<DatePlan>> GetAllExpiredPlansAsync(DateTime thresholdTime);
    }
}
