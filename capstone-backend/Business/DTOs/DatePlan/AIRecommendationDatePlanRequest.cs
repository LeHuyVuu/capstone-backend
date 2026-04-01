using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class AIRecommendationDatePlanRequest
    {
        [JsonPropertyName("request_context")]
        public RequestContextDto RequestContext { get; set; } = new();

        [JsonPropertyName("couple_context")]
        public CoupleContextDto CoupleContext { get; set; } = new();

        [JsonPropertyName("venue_candidates")]
        public List<VenueCandidateDto> VenueCandidates { get; set; } = new();
    }

    public class RequestContextDto
    {
        [JsonPropertyName("planned_start_at")]
        public string PlannedStartAt { get; set; } = string.Empty;

        [JsonPropertyName("planned_end_at")]
        public string PlannedEndAt { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("estimated_budget")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal EstimatedBudget { get; set; }

        [JsonPropertyName("user_intent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AiExtractedIntentResponse? UserIntent { get; set; }

        [JsonPropertyName("raw_query")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RawQuery { get; set; } 
    }

    public class CoupleContextDto
    {
        [JsonPropertyName("ages")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? Ages { get; set; }

        [JsonPropertyName("relationship_duration_days")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RelationshipDurationDays { get; set; }

        [JsonPropertyName("personality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Personality { get; set; }

        [JsonPropertyName("mood")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Mood { get; set; }

        [JsonPropertyName("interests")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Interests { get; set; }
    }

    public class VenueCandidateDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("rating")]
        public decimal Rating { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new();
    }
}
