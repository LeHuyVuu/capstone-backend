using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class CreateDatePlanRequest
    {
        /// <example>Hẹn hò tối thứ 7</example>
        [Required(ErrorMessage = "Date plan title is required")]
        public string Title { get; set; } = null!;
        /// <example>Xem phim rồi đi ăn lẩu, nhớ đặt bàn trước</example>
        public string? Note { get; set; }
        [Required(ErrorMessage = "Start time is required")]
        public DateTime PlannedStartAt { get; set; }
        [Required(ErrorMessage = "End time is required")]
        public DateTime PlannedEndAt { get; set; }
        public decimal EstimatedBudget { get; set; } = 0m;
    }
}
