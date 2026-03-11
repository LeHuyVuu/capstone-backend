namespace capstone_backend.Business.DTOs.Voucher
{
    public class UpdateVoucherRequest
    {
        /// <example>Chào Hè Rực Rỡ 2026 - Đợt 2</example>
        public string? Title { get; set; }

        /// <example>Giảm ngay 70k cho đơn hàng từ 300k trở lên, áp dụng khung giờ tối.</example>
        public string? Description { get; set; }

        /// <example>FIXED_AMOUNT</example>
        public string? DiscountType { get; set; }

        /// <example>70000</example>
        public decimal? DiscountAmount { get; set; }

        /// <example>null</example>
        public decimal? DiscountPercent { get; set; }

        /// <example>20</example>
        public int? Quantity { get; set; }

        /// <example>2</example>
        public int? UsageLimitPerMember { get; set; }

        /// <example>[164, 172]</example>
        public List<int>? VenueLocationIds { get; set; }

        /// <example>2026-03-15T08:00:00</example>
        public DateTime? StartDate { get; set; }

        /// <example>2026-07-15T23:59:59</example>
        public DateTime? EndDate { get; set; }
    }
}
