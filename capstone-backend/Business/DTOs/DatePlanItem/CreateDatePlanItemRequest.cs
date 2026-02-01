using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.DatePlanItem
{
    public class CreateDatePlanItemRequest
    {
        [Required(ErrorMessage = "Venues cannot be empty")]
        public List<DatePlanItemRequest> Venues { get; set; }
    }

    public class DatePlanItemRequest
    {
        public int VenueLocationId { get; set; }
        /// <example>12:00:00</example>
        public TimeOnly? StartTime { get; set; }
        /// <example>14:00:00</example>
        public TimeOnly? EndTime { get; set; }
        /// <example>1</example>
        public int OrderIndex { get; set; }
        /// <example>Đặt bàn trước để có chỗ ngồi đẹp</example>
        public string? Note { get; set; }
    }
}
