namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherItemValidationAndRedemptionResponse
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public string Status { get; set; } = null!;
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; } = null!;

        public string VoucherTitle { get; set; } = null!;
        public string VoucherDescription { get; set; } = null!;
        public string DiscountType { get; set; } = null!;
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }

        public DateTime? AcquiredAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public VoucherItemMemberBriefResponse? Member { get; set; }
    }
}
