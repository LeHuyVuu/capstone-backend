using Newtonsoft.Json;

namespace capstone_backend.Api.VenueRecommendation.Api.DTOs;

/// <summary>
/// Geo point for Meilisearch (_geo field requires lat/lng format)
/// </summary>
public class GeoPoint
{
    [JsonProperty("lat")]
    public double Lat { get; set; }

    [JsonProperty("lng")]
    public double Lng { get; set; }
}

/// <summary>
/// Venue location query result document for Meilisearch
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

    /// <summary>
    /// Geo point for Meilisearch geo search (format: {lat, lng})
    /// </summary>
    [JsonProperty("_geo")]
    public GeoPoint? Geo { get; set; }

    /// <summary>
    /// Distance in meters from search point (returned by Meilisearch when using _geoPoint sort)
    /// </summary>
    [JsonProperty("_geoDistance")]
    public int? GeoDistance { get; set; }

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
    public long? CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public long? UpdatedAt { get; set; }

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

    [JsonProperty("isOpenNow")]
    public bool? IsOpenNow { get; set; }

    [JsonProperty("todayOpenTime")]
    public string? TodayOpenTime { get; set; }

    [JsonProperty("todayCloseTime")]
    public string? TodayCloseTime { get; set; }

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
