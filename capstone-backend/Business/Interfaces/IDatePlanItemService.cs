using capstone_backend.Business.DTOs.DatePlanItem;
using System;

namespace capstone_backend.Business.Interfaces
{
    public interface IDatePlanItemService
    {
        Task<int> AddVenuesToDatePlanAsync(int userId, int datePlanId, CreateDatePlanItemRequest request);
        Task<DatePlanItemResponse> UpdateItemAsync(int userId, int datePlanId, int version, int datePlanItemId, UpdateDatePlanItemRequest request);
    }
}
