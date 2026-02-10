namespace capstone_backend.Business.DTOs.Recommendation;

/// <summary>
/// Request DTO for AI-powered venue recommendations
/// Supports both structured data and natural language queries
/// All fields are optional - AI will make recommendations with whatever info is provided
/// </summary>
public class RecommendationRequest
{
    /// <summary>
    /// Natural language query (e.g., "Hôm nay anniversary thì đi đâu?", "Muốn đi cafe yên tĩnh")
    /// AI will parse this to understand intent, mood, and preferences
    /// </summary>
    public string? Query { get; set; }
    
    /// <summary>
    /// Optional: User's MBTI personality type (e.g., "INTJ", "ESFP")
    /// </summary>
    public string? MbtiType { get; set; }
    
    /// <summary>
    /// Optional: User's current mood ID from MoodType table
    /// </summary>
    public int? MoodId { get; set; }
    
    /// <summary>
    /// Optional: Partner's MBTI personality type for couple recommendations
    /// </summary>
    public string? PartnerMbtiType { get; set; }
    
    /// <summary>
    /// Optional: Partner's current mood ID for couple recommendations
    /// </summary>
    public int? PartnerMoodId { get; set; }
    
    /// <summary>
    /// Optional: Province code for location filtering
    /// Examples: "01" = Hà Nội, "79" = TP.HCM, "92" = Cần Thơ, "48" = Đà Nẵng
    /// Only used when Latitude/Longitude are NOT provided
    /// </summary>
    public string? Area { get; set; }
    
    /// <summary>
    /// Latitude for geo-location search (use with Longitude)
    /// If provided, will search venues within radius instead of using Region
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude for geo-location search (use with Latitude)
    /// If provided, will search venues within radius instead of using Region
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Search radius in kilometers (default: 5km)
    /// Only used when Latitude/Longitude are provided
    /// </summary>
    public decimal? RadiusKm { get; set; } = 5;
    
    /// <summary>
    /// Optional: Budget level (1=Low, 2=Medium, 3=High)
    /// </summary>
    public int? BudgetLevel { get; set; }
    
    // Internal properties - set from query parameters, not from request body
    internal int Page { get; set; } = 1;
    internal int PageSize { get; set; } = 10;
    
}
