namespace capstone_backend.Business.DTOs.Momo
{
    public class MomoIpnRequest
    {
        public string? OrderType { get; set; }
        public string? OrderInfo { get; set; }
        public string? OrderId { get; set; }
        public long Amount { get; set; }
        public string? PartnerCode { get; set; }
        public long TransId { get; set; }
        public string? Signature { get; set; }
        public string? Message { get; set; }
        public string? PayType { get; set; }
        public string? RequestId { get; set; }
        public int ResultCode { get; set; }
        public long ResponseTime { get; set; }
        public string? ExtraData { get; set; }
    }
}
