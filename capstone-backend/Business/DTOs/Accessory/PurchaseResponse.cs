namespace capstone_backend.Business.DTOs.Accessory
{
    public class PurchaseResponse
    {
        public int PurchaseId { get; set; }
        public int AccessoryId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int PricePoint { get; set; }
        public int CouplePointRemaining { get; set; }
        public List<int> GrantedMemberIds { get; set; } = new List<int>();
        public DateTime? PurchasedAt { get; set; }
    }
}
