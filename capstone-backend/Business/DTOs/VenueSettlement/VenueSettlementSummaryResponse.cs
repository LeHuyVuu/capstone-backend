namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class VenueSettlementSummaryResponse
    {
        public decimal PendingAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal CancelledAmount { get; set; }

        public int PendingCount { get; set; }
        public int PaidCount { get; set; }
        public int CancelledCount { get; set; }
    }
}
