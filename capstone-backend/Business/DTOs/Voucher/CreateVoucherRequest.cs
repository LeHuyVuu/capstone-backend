using capstone_backend.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Voucher
{
    public class CreateVoucherRequest
    {
        /// <example>Chào Hè Rực Rỡ 2026</example>
        [Required]
        public string Title { get; set; } = null!;

        /// <example>Giảm ngay 50k cho đơn hàng từ 200k trở lên.</example>
        [Required]
        public string Description { get; set; } = null!;

        /// <example>FIXED_AMOUNT</example>
        [Required]
        public string DiscountType { get; set; } = null!;

        /// <example>50000</example>
        public decimal? DiscountAmount { get; set; }

        /// <example>null</example>
        public decimal? DiscountPercent { get; set; }

        /// <example>10</example>
        public int Quantity { get; set; } = 1;

        /// <example>1</example>
        public int? UsageLimitPerMember { get; set; }

        /// <example>7</example>
        public int UsageValidDays { get; set; } = 7;

        /// <example>[164]</example>
        public List<int> VenueLocationIds { get; set; } = new List<int>();

        public DateTime? StartDate { get; set; }

        /// <example>2026-06-11T23:59:59</example>
        public DateTime? EndDate { get; set; }
    }
}
