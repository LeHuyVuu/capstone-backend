namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherTransactionDetailResponse
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public int Quantity { get; set; }
        public int TotalPointsUsed { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<MemberVoucherTransactionVoucherItemResponse> VoucherItems { get; set; } = new();

    }
}
