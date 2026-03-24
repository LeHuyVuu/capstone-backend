namespace capstone_backend.Business.DTOs.Accessory
{
    public class PurchaseHistoryResponse
    {
        public int PurchaseId { get; set; }
        public int AccessoryId { get; set; }
        public int CoupleId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ResourceUrl { get; set; }
        public int? PricePoint { get; set; }
        public int PurchasedByMemberId { get; set; }
        public string? PurchasedByMemberName { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }
    }
}
