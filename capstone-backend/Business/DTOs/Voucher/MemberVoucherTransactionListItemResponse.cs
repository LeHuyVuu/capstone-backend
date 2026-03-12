namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherTransactionListItemResponse
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int TotalPointsUsed { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }

        public int VoucherTypeCount { get; set; }
        public List<string> VoucherTitles { get; set; } = new();
    }
}
