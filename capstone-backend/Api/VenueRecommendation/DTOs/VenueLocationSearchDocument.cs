using Newtonsoft.Json;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Venue location document for Meilisearch indexing
/// Contains all searchable and filterable fields
/// </summary>
public class VenueLocationQueryResult
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("address")]
    public string Address { get; set; } = null!;

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonProperty("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [JsonProperty("priceMin")]
    public decimal? PriceMin { get; set; }

    [JsonProperty("priceMax")]
    public decimal? PriceMax { get; set; }

    [JsonProperty("latitude")]
    public decimal? Latitude { get; set; }

    [JsonProperty("longitude")]
    public decimal? Longitude { get; set; }

    [JsonProperty("area")]
    public string? Area { get; set; }

    [JsonProperty("averageRating")]
    public decimal? AverageRating { get; set; }

    [JsonProperty("avarageCost")]
    public decimal? AvarageCost { get; set; }

    [JsonProperty("reviewCount")]
    public int? ReviewCount { get; set; }

    [JsonProperty("favoriteCount")]
    public int? FavoriteCount { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("coverImage")]
    public List<string>? CoverImage { get; set; }

    [JsonProperty("interiorImage")]
    public List<string>? InteriorImage { get; set; }

    [JsonProperty("category")]
    public string? Category { get; set; }

    [JsonProperty("fullPageMenuImage")]
    public List<string>? FullPageMenuImage { get; set; }

    [JsonProperty("isOwnerVerified")]
    public bool? IsOwnerVerified { get; set; }

    [JsonProperty("createdAt")]
    public long? CreatedAt { get; set; }  // Unix timestamp for filtering

    [JsonProperty("updatedAt")]
    public long? UpdatedAt { get; set; }  // Unix timestamp for filtering

    // Additional fields for search and filtering
    [JsonProperty("coupleMoodTypeIds")]
    public List<int>? CoupleMoodTypeIds { get; set; }

    [JsonProperty("coupleMoodTypeNames")]
    public List<string>? CoupleMoodTypeNames { get; set; }

    [JsonProperty("couplePersonalityTypeIds")]
    public List<int>? CouplePersonalityTypeIds { get; set; }

    [JsonProperty("couplePersonalityTypeNames")]
    public List<string>? CouplePersonalityTypeNames { get; set; }

    [JsonProperty("venueOwnerName")]
    public string? VenueOwnerName { get; set; }

    [JsonProperty("venueOwnerId")]
    public int VenueOwnerId { get; set; }

    // Today's opening status
    [JsonProperty("isOpenNow")]
    public bool? IsOpenNow { get; set; }

    [JsonProperty("todayOpenTime")]
    public string? TodayOpenTime { get; set; }

    [JsonProperty("todayCloseTime")]
    public string? TodayCloseTime { get; set; }

    // Recommendation-specific fields
    [JsonProperty("locationTagId")]
    public int? LocationTagId { get; set; }

    [JsonProperty("distance")]
    public decimal? Distance { get; set; }

    [JsonProperty("distanceText")]
    public string? DistanceText { get; set; }

    [JsonProperty("matchReason")]
    public string? MatchReason { get; set; }

    [JsonProperty("matchedTags")]
    public List<string>? MatchedTags { get; set; }
}
