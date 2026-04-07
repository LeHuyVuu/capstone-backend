using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Advertisement;

public class SubmitAdvertisementWithPaymentRequest
{
    [Required(ErrorMessage = "PackageId là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "PackageId phải lớn hơn 0")]
    public int PackageId { get; set; }

    [Required(ErrorMessage = "VenueIds là bắt buộc")]
    [MinLength(1, ErrorMessage = "Cần ít nhất một VenueId")]
    public List<int> VenueIds { get; set; } = new List<int>();

    /// <summary>
    /// Payment method: VIETQR (default) or WALLET
    /// </summary>
    public string PaymentMethod { get; set; } = "VIETQR";
}
