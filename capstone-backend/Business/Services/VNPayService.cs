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

        public async Task<VNPayReturnDto?> VerifyPaymentProcessing(IQueryCollection requestData)
        {
            var vnp_SecureHash = requestData["vnp_SecureHash"].ToString();

            if (string.IsNullOrEmpty(vnp_SecureHash))
                return null;

            var vnpayData = new SortedList<string, string>(new VnPayCompare());

            foreach (var key in requestData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                {
                    vnpayData.Add(key, requestData[key].ToString());
                }
            }

            var signData = new StringBuilder();
            foreach (var kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    signData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var signDataString = signData.ToString();
            if (signDataString.Length > 0)
            {
                signDataString = signDataString.Remove(signDataString.Length - 1, 1);
            }

            var myChecksum = HmacSHA512(_hashSecret, signDataString);

            if (myChecksum.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase))
            {
                return new VNPayReturnDto
                {
                    RspCode = requestData["vnp_ResponseCode"].ToString(),
                    Message = GetResponseMessage(requestData["vnp_ResponseCode"].ToString())
                };
            }
            else
            {
                return new VNPayReturnDto
                {
                    RspCode = "97",
                    Message = "Chữ ký không hợp lệ."
                };
            }
        }

        private static string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ.",
                "09" => "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking.",
                "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
                "11" => "Đã hết hạn chờ thanh toán.",
                "12" => "Thẻ/Tài khoản bị khóa.",
                "13" => "Nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Khách hàng hủy giao dịch.",
                "51" => "Tài khoản không đủ số dư.",
                "65" => "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định.",
                "99" => "Lỗi không xác định.",
                _ => $"Lỗi không xác định (mã: {responseCode})",
            };
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
