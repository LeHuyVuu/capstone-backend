namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Query request for venue locations
/// </summary>
public class VenueLocationQueryRequest
{
    /// <summary>
    /// Search query - searches in name, description, address, category
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of results per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by couple mood type IDs
    /// </summary>
    public List<int>? CoupleMoodTypeIds { get; set; }

    /// <summary>
    /// Filter by couple personality type IDs
    /// </summary>
    public List<int>? CouplePersonalityTypeIds { get; set; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by area/district
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Minimum average rating (1-5)
    /// </summary>
    public decimal? MinRating { get; set; }

    /// <summary>
    /// Maximum average rating (1-5)
    /// </summary>
    public decimal? MaxRating { get; set; }

    /// <summary>
    /// Minimum price
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter only verified venue owners
    /// </summary>
    public bool? OnlyVerified { get; set; }

    /// <summary>
    /// Filter only venues open now
    /// </summary>
    public bool? OnlyOpenNow { get; set; }

    /// <summary>
    /// Sort by field: "averageRating", "reviewCount", "createdAt", "priceMin", "favoriteCount"
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
