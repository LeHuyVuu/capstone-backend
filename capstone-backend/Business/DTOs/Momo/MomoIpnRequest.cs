namespace capstone_backend.Business.DTOs.Momo
{
    public class MomoIpnRequest
    {
        public string OrderType { get; set; }
        public string OrderInfo { get; set; }
        public string OrderId { get; set; }
        public bool Amount { get; set; }
        public string PartnerCode { get; set; }
        public string TransId { get; set; }
        public string Signature { get; set; }
        public string Message { get; set; }
        public string PayType { get; set; }
        public string RequestId { get; set; }
        public string ResultCode { get; set; }
        public string ResponseTime { get; set; }
        public long ExtraData { get; set; }
    }
}
