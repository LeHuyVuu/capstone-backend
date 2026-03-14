namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherSummaryResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;

        /// <summary>
        /// Tổng số lượng voucher được phát hành ban đầu
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// Số lượng voucher còn khả dụng
        /// </summary>
        public int RemainingQuantity { get; set; }

        /// <summary>
        /// Số lượng voucher đã được member nhận
        /// </summary>
        public int AcquiredCount { get; set; }

        /// <summary>
        /// Số lượng voucher đã được sử dụng
        /// </summary>
        public int UsedCount { get; set; }

        /// <summary>
        /// Số lượng voucher item đã hết hạn
        /// </summary>
        public int ExpiredCount { get; set; }

        /// <summary>
        /// Số lượng voucher item bị kết thúc theo trạng thái ended của voucher
        /// </summary>
        public int EndedCount { get; set; }

        /// <summary>
        /// Số lượng voucher item đang còn khả dụng để sử dụng
        /// </summary>
        public int AvailableCount { get; set; }

        /// <summary>
        /// Tỷ lệ sử dụng voucher
        /// FE nên hiển thị dưới dạng phần trăm
        /// </summary>
        public decimal UsageRate { get; set; }

        public int PointPrice { get; set; }
        public int TotalPointsExchanged { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
