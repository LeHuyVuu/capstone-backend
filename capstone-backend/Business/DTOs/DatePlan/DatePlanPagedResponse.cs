using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanPagedResponse
    {
        public PagedResult<DatePlanResponse> PagedResult { get; set; }
        public int TotalUpcoming { get; set; }
    }
}
