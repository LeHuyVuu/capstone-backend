using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.Zalo
{
    public class ZaloPayCallbackRequest
    {
        [JsonPropertyName("data")]
        public string Data { get; set; } = null!;

        [JsonPropertyName("mac")]
        public string Mac { get; set; } = null!;

        [JsonPropertyName("type")]
        public int Type { get; set; }
    }

    public class ZaloPayCallbackData
    {
        [JsonPropertyName("app_id")]
        public int AppId { get; set; }

        [JsonPropertyName("app_trans_id")]
        public string AppTransId { get; set; } = null!;

        [JsonPropertyName("app_time")]
        public long AppTime { get; set; }

        [JsonPropertyName("app_user")]
        public string AppUser { get; set; } = null!;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("embed_data")]
        public string EmbedData { get; set; } = null!;

        [JsonPropertyName("item")]
        public string Item { get; set; } = null!;

        [JsonPropertyName("zp_trans_id")]
        public long ZpTransId { get; set; }

        [JsonPropertyName("server_time")]
        public long ServerTime { get; set; }

        [JsonPropertyName("channel")]
        public int Channel { get; set; }

        [JsonPropertyName("merchant_user_id")]
        public string MerchantUserId { get; set; } = null!;

        [JsonPropertyName("user_fee_amount")]
        public long UserFeeAmount { get; set; }

        [JsonPropertyName("discount_amount")]
        public long DiscountAmount { get; set; }
    }
}
