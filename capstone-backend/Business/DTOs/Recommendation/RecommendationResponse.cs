using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.Recommendation;

public class RecommendationResponse
{
    public List<RecommendationItem> Recommendations { get; set; } = new();
}

public class RecommendationItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("score")]
    public int Score { get; set; }
    
    [JsonPropertyName("mood_tags")]
    public List<string> MoodTags { get; set; } = new();
    
    [JsonPropertyName("min_budget")]
    public long MinBudget { get; set; }
    
    [JsonPropertyName("max_budget")]
    public long MaxBudget { get; set; }
    
    [JsonPropertyName("weather_tags")]
    public List<string> WeatherTags { get; set; } = new();
    
    [JsonPropertyName("intimacy")]
    public string Intimacy { get; set; } = string.Empty;
    
    [JsonPropertyName("energy")]
    public string Energy { get; set; } = string.Empty;
    
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
