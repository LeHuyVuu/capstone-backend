namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherTransactionVoucherItemResponse
    {
        public int VoucherItemId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? AcquiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }

        public int VoucherId { get; set; }
        public string VoucherTitle { get; set; } = null!;
        public string? VoucherDescription { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}
