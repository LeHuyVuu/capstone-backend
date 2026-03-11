namespace capstone_backend.Business.DTOs.Voucher
{
    public class VoucherSummaryResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;

        public int TotalQuantity { get; set; }
        public int RemainingQuantity { get; set; }

        public int AcquiredCount { get; set; }
        public int UsedCount { get; set; }
        public int ExpiredCount { get; set; }
        public int AvailableCount { get; set; }

        public decimal UsageRate { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
