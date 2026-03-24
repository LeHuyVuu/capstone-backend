namespace capstone_backend.Business.DTOs.Accessory
{
    public class AccessoryDetailResponse
    {
        public int AccessoryId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string? ResourceUrl { get; set; }
        public int? PricePoint { get; set; }
        public bool? IsLimited { get; set; }

        public int? TotalQuantity { get; set; }
        public int? RemainingQuantity { get; set; }

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }

        public bool IsOwnedByMe { get; set; }
        public bool IsOwnedByPartner { get; set; }
        public bool CanPurchase { get; set; }
    }
}
