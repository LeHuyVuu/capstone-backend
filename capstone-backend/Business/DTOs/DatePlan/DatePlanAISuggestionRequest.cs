using capstone_backend.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanAISuggestionRequest
    {
        /// <example>Tối nay muốn đi date nhẹ nhàng, ăn tối rồi đi cafe yên tĩnh</example>
        public string? Query { get; set; }


        [Required(ErrorMessage = "Start time is required")]
        public DateTime PlannedStartAt { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public DateTime PlannedEndAt { get; set; }

        /// <example>SAME_DAY</example>
        public DatePlanDurationMode DurationMode { get; set; } = DatePlanDurationMode.SAME_DAY;

        /// <summary>
        /// Address
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Latitude for geo-location search (use with Longitude). If provided, geo search will be used.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Longitude for geo-location search (use with Latitude). If provided, geo search will be used.
        /// </summary>
        public decimal? Longitude { get; set; }

        public decimal EstimatedBudget { get; set; } = 0m;
    }
}
