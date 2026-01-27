namespace capstone_backend.Business.DTOs.Recommendation;

/// <summary>
/// Response DTO containing AI-generated venue recommendations
/// </summary>
public class RecommendationResponse
{
    /// <summary>
    /// List of recommended venues with scores
    /// </summary>
    public List<RecommendedVenue> Recommendations { get; set; } = new();
    
    /// <summary>
    /// AI-generated explanation for the recommendations
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Detected couple mood type based on input moods
    /// </summary>
    public string? CoupleMoodType { get; set; }
    
    /// <summary>
    /// Detected personality tags based on MBTI types
    /// </summary>
    public List<string> PersonalityTags { get; set; } = new();
    
    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Individual venue recommendation with scoring details
/// </summary>
public class RecommendedVenue
{
    /// <summary>
    /// Venue location ID
    /// </summary>
    public int VenueLocationId { get; set; }
    
    /// <summary>
    /// Venue name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Venue address
    /// </summary>
    public string Address { get; set; } = string.Empty;
    
    /// <summary>
    /// Venue description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Overall recommendation score (0-100)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Match reason from AI analysis
    /// </summary>
    public string MatchReason { get; set; } = string.Empty;
    
    /// <summary>
    /// Average rating from reviews
    /// </summary>
    public decimal? AverageRating { get; set; }
    
    /// <summary>
    /// Total number of reviews
    /// </summary>
    public int ReviewCount { get; set; }
    


    /// <summary>
    /// Cover image for the venue
    /// </summary>
    public string? CoverImage { get; set; }

    /// <summary>
    /// Interior image of the venue
    /// </summary>
    public string? InteriorImage { get; set; }

    /// <summary>
    /// Full page menu image
    /// </summary>
    public string? FullPageMenuImage { get; set; }
    
    /// <summary>
    /// Matched location tags
    /// </summary>
    public List<string> MatchedTags { get; set; } = new();
}
