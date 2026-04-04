
using System.Text.Json.Serialization;

namespace capstone_backend.Business.DTOs.Zalo
{
    public class ZaloPayLinkResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; set; } = null!;

        [JsonPropertyName("sub_return_code")]
        public int SubReturnCode { get; set; }

        [JsonPropertyName("sub_return_message")]
        public string SubReturnMessage { get; set; } = null!;

        [JsonPropertyName("order_url")]
        public string OrderUrl { get; set; } = null!;

        [JsonPropertyName("zp_trans_token")]
        public string ZpTransToken { get; set; } = null!;

        [JsonPropertyName("order_token")]
        public string OrderToken { get; set; } = null!;

        [JsonPropertyName("qr_code")]
        public string QrCode { get; set; } = null!;
    }
}
