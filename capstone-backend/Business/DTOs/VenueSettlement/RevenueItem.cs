namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class RevenueItem
    {
        public string Label { get; set; } = default!;
        public decimal Revenue { get; set; }
        public int Count { get; set; }
    }
}
