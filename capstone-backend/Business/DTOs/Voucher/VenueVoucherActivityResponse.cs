namespace capstone_backend.Business.DTOs.Voucher
{
    public class VenueVoucherActivityResponse
    {
        public int VoucherItemId { get; set; }
        public int VoucherId { get; set; }
        public int? VoucherItemMemberId { get; set; }

        public string ItemCode { get; set; } = null!;
        public string? Status { get; set; }

        public int? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberEmail { get; set; }
        public string? MemberPhone { get; set; }

        public int? Quantity { get; set; }
        public int? TotalPointsUsed { get; set; }
        public string? Note { get; set; }

        public DateTime? AcquiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
