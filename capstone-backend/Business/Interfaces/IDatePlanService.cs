using capstone_backend.Business.DTOs.DatePlan;

namespace capstone_backend.Business.Interfaces
{
    public interface IDatePlanService
    {
        Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request);
    }
}
