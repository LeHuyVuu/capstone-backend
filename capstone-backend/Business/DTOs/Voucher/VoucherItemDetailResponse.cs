namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherItemDetailResponse
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public string ItemCode { get; set; } = null!;
        public string Status { get; set; } = null!;

        public bool IsAssigned { get; set; }
        public DateTime? AcquiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public VoucherItemMemberBriefResponse? Member { get; set; }
    }
}
