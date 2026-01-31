using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlan;

namespace capstone_backend.Business.Interfaces
{
    public interface IDatePlanService
    {
        Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request);
        Task<PagedResult<DatePlanResponse>> GetAllDatePlansByTimeAsync(int pageNumber, int pageSize, int userId, string time);
        Task<DatePlanDetailResponse> GetByIdAsync(int datePlanId, int userId);
    }
}
