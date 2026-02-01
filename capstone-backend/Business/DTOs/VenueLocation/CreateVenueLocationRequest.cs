namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Request model for registering a new venue location
/// </summary>
public class CreateVenueLocationRequest
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Address { get; set; } = null!;

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

    /// <summary>
    /// Couple mood type ID to determine location tag
    /// </summary>
    public int? CoupleMoodTypeId { get; set; }

    /// <summary>
    /// Couple personality type ID to determine location tag
    /// </summary>
    public int? CouplePersonalityTypeId { get; set; }
}
