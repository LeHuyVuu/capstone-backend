namespace capstone_backend.Data.Entities
{
    public partial class VenueSettlement
    {
        public int Id { get; set; }

        public int VoucherItemId { get; set; }
        public int? VoucherItemMemberId { get; set; }
        public int VenueOwnerId { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string Status { get; set; } = null!;

        public DateTime? AvailableAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
