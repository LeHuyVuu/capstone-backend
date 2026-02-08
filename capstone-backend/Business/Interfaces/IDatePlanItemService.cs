using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.DatePlanItem;
using System;

namespace capstone_backend.Business.Interfaces
{
    public interface IDatePlanItemService
    {
        Task<int> AddVenuesToDatePlanAsync(int userId, int datePlanId, CreateDatePlanItemRequest request);
        Task<DatePlanItemResponse> UpdateItemAsync(int userId, int datePlanId, int version, int datePlanItemId, UpdateDatePlanItemRequest request);
        Task<DatePlanItemResponse> GetDetailDatePlanItemAsync(int userId, int datePlanItemId, int datePlanId);
        Task<PagedResult<DatePlanItemResponse>> GetAllAsync(int pageNumber, int pageSize, int userId, int datePlanId);
        Task<int> DeleteDatePlanItemAsync(int value, int datePlanItemId, int datePlanId);
        Task<bool> ReorderDatePlanItemAsync(int userId, int datePlanId, ReorderDatePlanItemsRequest request);
    }
}
