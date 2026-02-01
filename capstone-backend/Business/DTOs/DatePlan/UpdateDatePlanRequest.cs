namespace capstone_backend.Business.DTOs.DatePlan
{
    public class UpdateDatePlanRequest
    {
        public string? Title { get; set; }
        public string? Note { get; set; }
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public decimal? EstimatedBudget { get; set; }
    }
}
