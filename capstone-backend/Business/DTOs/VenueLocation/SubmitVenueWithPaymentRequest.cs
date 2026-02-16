using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.VenueLocation;

public class SubmitVenueWithPaymentRequest
{
    [Required(ErrorMessage = "PackageId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId must be greater than 0")]
    public int PackageId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 12, ErrorMessage = "Quantity must be between 1 and 12")]
    public int Quantity { get; set; } = 1;
}
