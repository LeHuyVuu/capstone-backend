namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class VenueSettlementDetailResponse
    {
        public int SettlementId { get; set; }
        public int VoucherItemId { get; set; }
        public int? VoucherItemMemberId { get; set; }

        public string? VoucherItemCode { get; set; } = null!;
        public string? VoucherTitle { get; set; } = null!;
        public string? MemberName { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? UsedAt { get; set; }
        public DateTime? AvailableAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
