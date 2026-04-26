namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class RevenueRequest
    {
        /// <summary>2026-01-01</summary>
        public DateTime? FromDate { get; set; }
        /// <summary>2026-12-31</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// day | month | year
        /// </summary>
        /// <example>month</example>
        public string GroupBy { get; set; } = "month"; // day | month | year
    }
}
