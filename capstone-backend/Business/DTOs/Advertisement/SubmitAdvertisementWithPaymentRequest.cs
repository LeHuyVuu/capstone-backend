using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class SubmitAdvertisementWithPaymentRequest
{
    [Required(ErrorMessage = "PackageId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId must be greater than 0")]
    public int PackageId { get; set; }

    [Required(ErrorMessage = "VenueIds is required")]
    [MinLength(1, ErrorMessage = "At least one VenueId is required")]
    public List<int> VenueIds { get; set; } = new List<int>();

    /// <summary>
    /// Payment method: VIETQR (default) or WALLET
    /// </summary>
    public string PaymentMethod { get; set; } = "VIETQR";
}
