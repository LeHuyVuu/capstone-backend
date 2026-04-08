using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class CreateDatePlanRequest
    {
        /// <example>Hẹn hò tối thứ 7</example>
        public string Title { get; set; } = null!;
        /// <example>Xem phim rồi đi ăn lẩu, nhớ đặt bàn trước</example>
        public string? Note { get; set; }
        public DateTime PlannedStartAt { get; set; }
        public DateTime PlannedEndAt { get; set; }
        public decimal EstimatedBudget { get; set; } = 0m;

        /// <example>SAME_DAY</example>
        public DatePlanDurationMode DurationMode { get; set; } = DatePlanDurationMode.SAME_DAY;
    }
}
