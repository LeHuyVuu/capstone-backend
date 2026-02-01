namespace capstone_backend.Business.DTOs.DatePlan
{
    public class UpdateDatePlanRequest
    {
        /// <example>Buổi hẹn thứ 100</example>
        public string? Title { get; set; }
        /// <example>Đi chơi ở Đà Lạt dịp cuối năm</example>
        public string? Note { get; set; }
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public decimal? EstimatedBudget { get; set; }
    }
}
