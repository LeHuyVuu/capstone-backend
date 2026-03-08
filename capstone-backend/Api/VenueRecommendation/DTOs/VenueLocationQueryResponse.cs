using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Api.VenueRecommendation.Api.DTOs;

/// <summary>
/// Query response for venue locations with pagination and facets
/// </summary>
public class VenueLocationQueryResponse
{
    /// <summary>
    /// Paginated query recommendations
    /// </summary>
    public PagedResult<VenueLocationQueryResult> Recommendations { get; set; } = null!;

    /// <summary>
    /// Explanation of search results
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Couple mood type used for search context
    /// </summary>
    public string? CoupleMoodType { get; set; }

    /// <summary>
    /// Personality tags used for search context
    /// </summary>
    public List<string>? PersonalityTags { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Search query used
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Facet distributions for filtering (deprecated, kept for backward compatibility)
    /// </summary>
    public SearchFacets? Facets { get; set; }
}

/// <summary>
/// Facet distributions from search results
/// </summary>
public class SearchFacets
{
    public Dictionary<string, int>? Categories { get; set; }
    public Dictionary<string, int>? Areas { get; set; }
    public Dictionary<string, int>? CoupleMoodTypes { get; set; }
    public Dictionary<string, int>? CouplePersonalityTypes { get; set; }
}
