namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherItemMemberBriefResponse
    {
        public int MemberId { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
