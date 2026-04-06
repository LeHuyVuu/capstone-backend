using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.VNPay;
using capstone_backend.Business.Interfaces;
using capstone_backend.Extensions.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace capstone_backend.Business.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly string _baseUrl = Environment.GetEnvironmentVariable("VNPAY_ENDPOINT") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private readonly string _tmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE");
        private readonly string _hashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET");
        private readonly string _returnUrl = "https://kusl.io.vn/payment-result";

        public VNPayService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<VNPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request)
        {
            var now = DateTime.UtcNow;
            var nowVn = TimezoneUtil.ToVietNamTime(now);

            var vnpayParams = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", $"{99000 * 100}" },
                { "vnp_CreateDate", nowVn.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", GetIpAddress() },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan goi 1" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _returnUrl },
                //{ "vnp_ExpireDate", nowVn.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                { "vnp_TxnRef", $"{DateTime.Now.Ticks}" }
            };

            var response = new VNPayLinkResponse
            {
                PayUrl = GenerateVnPayUrl(vnpayParams, _baseUrl, _hashSecret)
            };

            return response;
        }

        private string GenerateVnPayUrl(SortedList<string, string> parameters, string baseUrl, string hashSecret)
        {
            var hashData = new StringBuilder();
            var query = new StringBuilder();

            foreach (var kv in parameters)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    continue;

                if (hashData.Length > 0)
                    hashData.Append('&');

                hashData.Append(WebUtility.UrlEncode(kv.Key));
                hashData.Append('=');
                hashData.Append(WebUtility.UrlEncode(kv.Value));

                query.Append(WebUtility.UrlEncode(kv.Key));
                query.Append('=');
                query.Append(WebUtility.UrlEncode(kv.Value));
                query.Append('&');
            }

            var vnpSecureHash = HmacSHA512(hashSecret, hashData.ToString());
            query.Append("vnp_SecureHash=");
            query.Append(vnpSecureHash);

            return $"{baseUrl}?{query.ToString()}";
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private string GetIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "127.0.0.1";
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            return string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = string.CompareOrdinal(x, y);
            return vnpCompare;
        }
    }
}
