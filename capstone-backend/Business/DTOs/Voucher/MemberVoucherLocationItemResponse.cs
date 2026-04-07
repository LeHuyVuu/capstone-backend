namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherLocationItemResponse
    {
        public int VenueLocationId { get; set; }
        public string VenueLocationName { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
