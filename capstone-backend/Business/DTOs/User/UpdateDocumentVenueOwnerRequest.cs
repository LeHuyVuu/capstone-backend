using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.User;

public class UpdateDocumentVenueOwnerRequest
{
    [Required]
    public string CitizenIdFrontUrl { get; set; } = null!;

    [Required]
    public string CitizenIdBackUrl { get; set; } = null!;

    [Required]
    public string BusinessLicenseUrl { get; set; } = null!;
}
