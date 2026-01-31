namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Request model for updating venue location information
/// </summary>
public class UpdateVenueLocationRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? WebsiteUrl { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>
    /// Cover image URLs (max 5)
    /// </summary>
    public List<string>? CoverImage { get; set; }

    /// <summary>
    /// Interior image URLs (max 5)
    /// </summary>
    public List<string>? InteriorImage { get; set; }

    /// <summary>
    /// Menu image URLs (max 5)
    /// </summary>
    public List<string>? FullPageMenuImage { get; set; }

    /// <summary>
    /// Indicates if the venue owner is verified
    /// </summary>
    public bool? IsOwnerVerified { get; set; }
}
