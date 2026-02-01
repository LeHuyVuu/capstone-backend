namespace capstone_backend.Business.DTOs.User;

/// <summary>
/// Response model for venue owner profile data
/// </summary>
public class VenueOwnerProfileResponse
{
    public int Id { get; set; }
    public string? BusinessName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? CitizenIdFrontUrl { get; set; }
    public string? CitizenIdBackUrl { get; set; }
    public string? BusinessLicenseUrl { get; set; }
}
