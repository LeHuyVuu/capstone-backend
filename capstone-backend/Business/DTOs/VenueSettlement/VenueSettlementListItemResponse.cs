namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class VenueSettlementListItemResponse
    {
        public int SettlementId { get; set; }
        public int VoucherItemId { get; set; }
        public string VoucherItemCode { get; set; } = null!;
        public string VoucherTitle { get; set; } = null!;

        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? UsedAt { get; set; }
        public DateTime? AvailableAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? Note { get; set; }
    }
}
