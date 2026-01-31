namespace capstone_backend.Business.DTOs.Recommendation;

/// <summary>
/// Response DTO containing AI-generated venue recommendations
/// </summary>
public class RecommendationResponse
{
    /// <summary>
    /// List of recommended venues
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
    /// Location tag ID
    /// </summary>
    public int? LocationTagId { get; set; }
    
    /// <summary>
    /// Venue owner ID
    /// </summary>
    public int VenueOwnerId { get; set; }
    
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
    /// Venue email
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Venue phone number
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Venue website URL
    /// </summary>
    public string? WebsiteUrl { get; set; }
    
    /// <summary>
    /// Opening time
    /// </summary>
    public DateTime? OpeningTime { get; set; }
    
    /// <summary>
    /// Closing time
    /// </summary>
    public DateTime? ClosingTime { get; set; }
    
    /// <summary>
    /// Whether venue is currently open
    /// </summary>
    public bool? IsOpen { get; set; }
    
    /// <summary>
    /// Minimum price
    /// </summary>
    public decimal? PriceMin { get; set; }
    
    /// <summary>
    /// Maximum price
    /// </summary>
    public decimal? PriceMax { get; set; }
    
    /// <summary>
    /// Venue latitude
    /// </summary>
    public decimal? Latitude { get; set; }
    
    /// <summary>
    /// Venue longitude
    /// </summary>
    public decimal? Longitude { get; set; }
    
    /// <summary>
    /// Venue area/region
    /// </summary>
    public string? Area { get; set; }
    
    /// <summary>
    /// Average cost per person
    /// </summary>
    public decimal? AvarageCost { get; set; }
    
    /// <summary>
    /// Venue status
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Venue category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Whether owner is verified
    /// </summary>
    public bool? IsOwnerVerified { get; set; }
    
    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether venue is deleted
    /// </summary>
    public bool? IsDeleted { get; set; }
    
    /// <summary>
    /// Distance from user's location in kilometers (null if no lat/lon provided)
    /// </summary>
    public decimal? Distance { get; set; }
    
    /// <summary>
    /// Formatted distance text (e.g., "500 m" or "2.3 km")
    /// </summary>
    public string? DistanceText { get; set; }
    
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
