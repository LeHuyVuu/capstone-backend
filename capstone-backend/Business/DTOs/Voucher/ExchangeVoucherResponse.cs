namespace capstone_backend.Business.DTOs.Voucher
{
    public class ExchangeVoucherResponse
    {
        public int VoucherItemMemberId { get; set; }
        public int MemberId { get; set; }
        public int TotalQuantityExchanged { get; set; }
        public int TotalPointsUsed { get; set; }
        public int RemainingPoints { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExchangeVoucherItemResult> VoucherItems { get; set; } = new();
    }

    public class ExchangeVoucherItemResult
    {
        public int VoucherId { get; set; }
        public string VoucherTitle { get; set; } = null!;
        public string ItemCode { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? AcquiredAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
