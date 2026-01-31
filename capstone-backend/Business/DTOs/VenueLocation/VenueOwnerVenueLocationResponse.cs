namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location list of a venue owner
/// Contains venue information with location tag details (couple mood type and personality type)
/// </summary>
public class VenueOwnerVenueLocationResponse
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
    public string? Area { get; set; }
    public decimal? AverageRating { get; set; }
    public decimal? AvarageCost { get; set; }
    public int? ReviewCount { get; set; }
    public string? Status { get; set; }
    public string? CoverImage { get; set; }
    public string? InteriorImage { get; set; }
    public string? Category { get; set; }
    public string? FullPageMenuImage { get; set; }
    public bool? IsOwnerVerified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Location Tag information including couple mood type and personality type
    /// </summary>
    public VenueOwnerLocationTagInfo? LocationTag { get; set; }
}

/// <summary>
/// Location tag information with full details for venue owner's venues
/// </summary>
public class VenueOwnerLocationTagInfo
{
    public int Id { get; set; }
    public string? TagName { get; set; }
    public string[]? DetailTag { get; set; }

    /// <summary>
    /// Couple mood type with full details
    /// </summary>
    public VenueOwnerCoupleMoodTypeInfo? CoupleMoodType { get; set; }

    /// <summary>
    /// Couple personality type with full details
    /// </summary>
    public VenueOwnerCouplePersonalityTypeInfo? CouplePersonalityType { get; set; }
}

/// <summary>
/// Couple mood type information for venue owner's venues
/// </summary>
public class VenueOwnerCoupleMoodTypeInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Couple personality type information for venue owner's venues
/// </summary>
public class VenueOwnerCouplePersonalityTypeInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
