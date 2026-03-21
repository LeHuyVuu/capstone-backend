namespace capstone_backend.Business.DTOs.Voucher
{
    public class MemberVoucherListItemResponse
    {
        public int Id { get; set; }
        public int? VenueOwnerId { get; set; }
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int PointPrice { get; set; }
        public string DiscountType { get; set; } = null!;
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int Quantity { get; set; }
        public int? RemainingQuantity { get; set; }
        public int? UsageLimitPerMember { get; set; }
        public int? RemainingUsagePerMember { get; set; }
        public int? UsageValiDays { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<MemberVoucherLocationItemResponse> Locations { get; set; } = new();
    }
}
