using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.VNPay
{
    public class VNPayIpnRequest
    {
        [JsonPropertyName("vnp_TmnCode")]
        public string TmnCode { get; set; } = null!;

        [JsonPropertyName("vnp_Amount")]
        public long Amount { get; set; }

        [JsonPropertyName("vnp_BankCode")]
        public string BankCode { get; set; } = null!;

        [JsonPropertyName("vnp_BankTranNo")]
        public string? BankTranNo { get; set; }

        [JsonPropertyName("vnp_CardType")]
        public string? CardType { get; set; }

        [JsonPropertyName("vnp_PayDate")]
        public int? PayDate { get; set; }

        [JsonPropertyName("vnp_OrderInfo")]
        public string OrderInfo { get; set; } = null!;

        [JsonPropertyName("vnp_TransactionNo")]
        public int TransactionNo { get; set; }

        [JsonPropertyName("vnp_ResponseCode")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("vnp_TransactionStatus")]
        public int TransactionStatus { get; set; }

        [JsonPropertyName("vnp_TxnRef")]
        public string TxnRef { get; set; } = null!;

        [JsonPropertyName("vnp_SecureHash")]
        public string SecureHash { get; set; } = null!;
    }
}
