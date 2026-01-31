using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanResponse
    {
        public int Id { get; set; }
        public int CoupleId { get; set; }
        public int OrganizerMemberId { get; set; }
        public string Title { get; set; }
        public string? Note { get; set; }
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalCount { get; set; } = 0;
        public int VisitedCount { get; set; } = 0;
        public decimal CompletionRate { get; set; } = 0m;
        public decimal? EstimatedBudget { get; set; }
        public string? Status { get; set; }
    }
}
