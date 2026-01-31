using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Version { get; set; }
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public decimal? EstimatedBudget { get; set; }
        public string? Status { get; set; }
    }

}
