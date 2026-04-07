namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherLocationItemResponse
    {
        public int VenueLocationId { get; set; }
        public string? VenueLocationName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
