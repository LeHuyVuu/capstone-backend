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
    /// Number of venue locations included in this submit request.
    /// Optional for backward compatibility. If provided, it must match distinct VenueIds count.
    /// </summary>
    [Range(1, 200, ErrorMessage = "Quantity phải nằm trong khoảng từ 1 đến 200")]
    public int? Quantity { get; set; }

    /// <summary>
    /// Payment method: VIETQR (default) or WALLET
    /// </summary>
    public string PaymentMethod { get; set; } = "VIETQR";
}
