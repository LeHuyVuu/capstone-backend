namespace capstone_backend.Business.DTOs.Momo
{
    public class MomoTransactionMetadata
    {
        public string? RequestId { get; set; }
        public string? OrderId { get; set; }
        public long? TransId { get; set; }

        public string? PayUrl { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? DeeplinkMiniApp { get; set; }
    }
}
