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
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int OrderIndex { get; set; }
        public string? Note { get; set; }
    }
}
