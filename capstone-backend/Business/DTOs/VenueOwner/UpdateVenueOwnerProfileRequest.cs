using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.VenueOwner;

public class UpdateVenueOwnerProfileRequest
{
    [MaxLength(200)]
    public string? BusinessName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    public string? CitizenIdFrontUrl { get; set; }

    public string? CitizenIdBackUrl { get; set; }
}
