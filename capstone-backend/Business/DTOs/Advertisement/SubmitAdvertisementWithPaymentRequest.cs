using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class SubmitAdvertisementWithPaymentRequest
{
    [Required(ErrorMessage = "PackageId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId must be greater than 0")]
    public int PackageId { get; set; }

    [Required(ErrorMessage = "VenueId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "VenueId must be greater than 0")]
    public int VenueId { get; set; }
}
