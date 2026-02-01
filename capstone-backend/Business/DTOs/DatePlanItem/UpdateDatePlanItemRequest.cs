namespace capstone_backend.Business.DTOs.DatePlanItem
{
    public class UpdateDatePlanItemRequest
    {
        /// <example>12:00:00</example>
        public TimeOnly? StartTime { get; set; }
        /// <example>14:00:00</example>
        public TimeOnly? EndTime { get; set; }
        /// <example>1</example>
        public int? OrderIndex { get; set; }
        /// <example>Đặt bàn trước để có chỗ ngồi đẹp</example>
        public string? Note { get; set; }
    }
}
