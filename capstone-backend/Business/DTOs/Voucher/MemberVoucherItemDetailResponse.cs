namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherItemDetailResponse
    {
        public int VoucherItemId { get; set; }
        public int VoucherId { get; set; }
        public string VoucherTitle { get; set; } = null!;
        public string? VoucherDescription { get; set; }
        public string ItemCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? AcquiredAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }

        public bool IsUsable { get; set; } = false;
        public string StatusNote = string.Empty;

        public List<MemberVoucherLocationItemResponse> Locations { get; set; } = new();
    }
}
