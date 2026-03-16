namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherItemResponse
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public string Status { get; set; } = null!;
        public bool IsAssigned { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
