using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.DTOs.DatePlanItem
{
    public class DatePlanItemResponse
    {
        public int Id { get; set; }
        public int DatePlanId { get; set; }
        public int VenueLocationId { get; set; }
        public VenueLocationDetailResponse VenueLocation { get; set; }
        public int? OrderIndex { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}
