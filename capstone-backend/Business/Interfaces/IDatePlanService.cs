using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Interfaces
{
    public interface IDatePlanService
    {
        Task<int> CreateDatePlanAsync(int userId, CreateDatePlanRequest request);
        Task<(PagedResult<DatePlanResponse>, int totalUpcoming)> GetAllDatePlansByTimeAsync(int pageNumber, int pageSize, int userId, string time);
        Task<DatePlanDetailResponse> GetByIdAsync(int datePlanId, int userId);
        Task<DatePlanResponse> UpdateDatePlanAsync(int userId, int datePlanId, int version, UpdateDatePlanRequest request);
        Task<int> DeleteDatePlanAsync(int userId, int datePlanId);
        Task<int> StartDatePlanAsync(int userId, int datePlanId);
        Task<int> ActionDatePlanAsync(int userId, int datePlanId, DatePlanAction action);
    }
}
