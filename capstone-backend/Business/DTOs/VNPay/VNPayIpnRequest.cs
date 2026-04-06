using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.VNPay
{
    public class VNPayIpnRequest
    {
        public string vnp_TmnCode { get; set; } = null!;
        public string vnp_Amount { get; set; } = null!;
        public string vnp_BankCode { get; set; } = null!;
        public string? vnp_BankTranNo { get; set; }
        public string? vnp_CardType { get; set; }
        public string? vnp_PayDate { get; set; }
        public string vnp_OrderInfo { get; set; } = null!;
        public string vnp_TransactionNo { get; set; } = null!;
        public string vnp_ResponseCode { get; set; } = null!;
        public string vnp_TransactionStatus { get; set; } = null!;
        public string vnp_TxnRef { get; set; } = null!;
        public string vnp_SecureHash { get; set; } = null!;
    }
}
