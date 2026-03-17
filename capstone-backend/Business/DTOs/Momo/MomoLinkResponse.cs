namespace capstone_backend.Business.DTOs.Momo
{
    public class MomoLinkResponse
    {
        public string? PayUrl { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? DeeplinkMiniApp { get; set; }
    }
}
