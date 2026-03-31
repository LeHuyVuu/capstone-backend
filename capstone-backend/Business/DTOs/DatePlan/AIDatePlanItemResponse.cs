using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class AIDatePlanItemResponse
    {
        [JsonPropertyName("items")]
        public List<AIDatePlanItemRequest> Items { get; set; } = new();
    }

    public class AIDatePlanItemRequest
    {
        [JsonPropertyName("venueLocationId")]
        public int VenueLocationId { get; set; }

        [JsonPropertyName("venueName")]
        public string? VenueName { get; set; }

        [JsonPropertyName("venueDescription")]
        public string? VenueDescription { get; set; }

        [JsonPropertyName("venueAddress")]
        public string? VenueAddress { get; set; }

        [JsonPropertyName("venueAverageRating")]
        public decimal? VenueAverageRating { get; set; }

        [JsonPropertyName("venueCoverImage")]
        public List<string>? VenueCoverImage { get; set; }

        [JsonPropertyName("startTime")]
        public TimeOnly? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public TimeOnly? EndTime { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }
}
