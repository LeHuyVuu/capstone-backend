using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Voucher
{
    public class ValidateAndRedeemVoucherItemRequest
    {

        [Required(ErrorMessage = "Mã voucher không được để trống")]
        public string ItemCode { get; set; } = null!;
        public int VenueLocationId { get; set; }
    }
}
