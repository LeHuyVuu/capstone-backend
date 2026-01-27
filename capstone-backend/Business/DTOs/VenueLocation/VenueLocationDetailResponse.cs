using capstone_backend.Business.DTOs.User;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location detail
/// </summary>
public class VenueLocationDetailResponse
{
    public int Id { get; set; }
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

    // Venue opening hours for each day
    public List<VenueOpeningHourResponse>? OpeningHours { get; set; }
}

/// <summary>
/// Location tag information including couple mood type and personality type
/// </summary>
public class LocationTagInfo
{
    public int Id { get; set; }
    public string? TagName { get; set; }
    public CoupleMoodTypeInfo? CoupleMoodType { get; set; }
    public CouplePersonalityTypeInfo? CouplePersonalityType { get; set; }
}

/// <summary>
/// Couple mood type information
/// </summary>
public class CoupleMoodTypeInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Couple personality type information
/// </summary>
public class CouplePersonalityTypeInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
