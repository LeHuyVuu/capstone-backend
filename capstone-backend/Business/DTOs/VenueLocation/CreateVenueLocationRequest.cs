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

    public DateTime? OpeningTime { get; set; }

    public DateTime? ClosingTime { get; set; }

    public bool? IsOpen { get; set; }

    public decimal? PriceMin { get; set; }

    public decimal? PriceMax { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>
    /// List of location tag IDs to associate with this venue
    /// </summary>
    public List<int> LocationTagIds { get; set; } = new();
}
