using capstone_backend.Business.DTOs.DatePlanItem;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanDetailResponse : DatePlanResponse
    {
        public int CoupleId { get; set; }
        public int OrganizerMemberId { get; set; }
        public string? Note { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<DatePlanItemResponse> Venues { get; set; } = new List<DatePlanItemResponse>();
    }
}
