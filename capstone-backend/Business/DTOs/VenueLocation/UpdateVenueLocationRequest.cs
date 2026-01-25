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

    public DateTime? OpeningTime { get; set; }

    public DateTime? ClosingTime { get; set; }

    public bool? IsOpen { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>
    /// Cover image URL for the venue
    /// </summary>
    public string? CoverImage { get; set; }

    /// <summary>
    /// Interior image URL for the venue
    /// </summary>
    public string? InteriorImage { get; set; }

    /// <summary>
    /// Full-page menu image URL for the venue
    /// </summary>
    public string? FullPageMenuImage { get; set; }

    /// <summary>
    /// Indicates if the venue owner is verified
    /// </summary>
    public bool? IsOwnerVerified { get; set; }
}
