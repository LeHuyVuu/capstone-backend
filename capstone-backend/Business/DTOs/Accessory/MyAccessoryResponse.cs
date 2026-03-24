
namespace capstone_backend.Business.DTOs.Accessory
{
    public class MyAccessoryResponse
    {
        public int MemberAccessoryId { get; set; }
        public int AccessoryId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ResourceUrl { get; set; }
        public bool IsEquipped { get; set; }
        public DateTime? AcquiredAt { get; set; }
    }
}
