using capstone_backend.Business.DTOs.User;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location with KYC documents and venue owner profile
/// </summary>
public class VenueLocationWithKycResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? WebsiteUrl { get; set; }
    public string? Status { get; set; }
    public string? BusinessLicenseUrl { get; set; }
    public VenueOwnerProfileResponse? VenueOwner { get; set; }
}
