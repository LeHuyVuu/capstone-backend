using capstone_backend.Business.DTOs.User;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location creation
/// </summary>
public class VenueLocationCreateResponse
{
    public int Id { get; set; }
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
    public decimal? AverageRating { get; set; }
    public decimal? AvarageCost { get; set; }
    public int? ReviewCount { get; set; }
    public string? Status { get; set; }
    public string? CoverImage { get; set; }
    public string? InteriorImage { get; set; }
    public string? FullPageMenuImage { get; set; }
    public bool? IsOwnerVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Location Tag information
    public LocationTagInfo? LocationTag { get; set; }

    // Venue Owner Profile information
    public VenueOwnerProfileResponse? VenueOwner { get; set; }
}
