using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.DatePlan
{
    public class AiExtractedIntentResponse
    {
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new();

        [JsonPropertyName("mood_tags")]
        public List<string> MoodTags { get; set; } = new();

        [JsonPropertyName("time_hint")]
        public string? TimeHint { get; set; }

        [JsonPropertyName("special_note")]
        public string? SpecialNote { get; set; }
    }
}
