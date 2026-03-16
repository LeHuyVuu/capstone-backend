namespace capstone_backend.Api.VenueRecommendation.Api.DTOs;

public sealed class MeilisearchPersonalizeTestRequest
{
    public string IndexUid { get; set; } = "venue_locations";
    public string? Q { get; set; }
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public string? Filter { get; set; } = "status = 'ACTIVE'";
    public string[]? Sort { get; set; }
    public bool UseHybrid { get; set; } = true;
    public double SemanticRatio { get; set; } = 0.2;
}