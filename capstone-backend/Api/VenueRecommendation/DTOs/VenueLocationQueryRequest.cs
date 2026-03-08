using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace capstone_backend.Api.VenueRecommendation.Api.DTOs;


public class VenueLocationQueryRequest
{

    public string? Query { get; set; }

    public int Page { get; set; } = 1;


    public int PageSize { get; set; } = 20;

    public string? Category { get; set; }


    public string? Area { get; set; }

    [JsonProperty("lat")]
    [JsonPropertyName("lat")]
    public decimal? Latitude { get; set; }

    [JsonProperty("lng")]
    [JsonPropertyName("lng")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Radius in kilometers for geo filtering.
    /// If not provided with lat/lng, will sort by distance without filtering.
    /// </summary>
    [JsonProperty("radiusKm")]
    [JsonPropertyName("radiusKm")]
    public decimal? RadiusKm { get; set; } 

    public decimal? MinRating { get; set; }


    public decimal? MaxRating { get; set; }

    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price
    /// </summary>
    public decimal? MaxPrice { get; set; }


    public bool? OnlyOpenNow { get; set; }

    public string? SortBy { get; set; }


    public string SortDirection { get; set; } = "desc";
}
