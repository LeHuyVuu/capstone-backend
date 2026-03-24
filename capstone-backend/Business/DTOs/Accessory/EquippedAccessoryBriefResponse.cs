namespace capstone_backend.Business.DTOs.Accessory
{
    public class EquippedAccessoryBriefResponse
    {
        public int MemberAccessoryId { get; set; }
        public int AccessoryId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ResourceUrl { get; set; }
    }
}
