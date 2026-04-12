namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location with KYC information
/// </summary>
public class VenueLocationWithKycResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? WebsiteUrl { get; set; } = string.Empty;
    public string? Address { get; set; }

    public string Status { get; set; } = null!;
    public string? BusinessLicenseUrl { get; set; }
    public VenueOwnerKycInfo VenueOwner { get; set; } = null!;

    public VenueLocationDetailResponse? Venue { get; set; }
}

/// <summary>
/// Venue owner KYC information
/// </summary>
public class VenueOwnerKycInfo
{
    public int Id { get; set; }
    public string? BusinessName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CitizenIdFrontUrl { get; set; }
    public string? CitizenIdBackUrl { get; set; }
    public string? BusinessLicenseUrl { get; set; }
}
