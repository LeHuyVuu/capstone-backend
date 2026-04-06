using AutoMapper;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.VNPay;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace capstone_backend.Business.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly string _baseUrl = Environment.GetEnvironmentVariable("VNPAY_ENDPOINT") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private readonly string _tmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE");
        private readonly string _hashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET");
        private readonly string _returnUrl = Environment.GetEnvironmentVariable("VNPAY_RETURN_URL");

        public VNPayService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        public async Task<VNPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) throw new Exception("Hồ sơ thành viên không tồn tại");

            if (request.PaymentMethod != PaymentMethod.VNPAY.ToString())
                throw new Exception("Phương thức thanh toán không hợp lệ");

            var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(request.PackageId);
            if (package == null || package.IsDeleted == true || package.IsActive == false)
                throw new Exception("Gói đăng ký không tồn tại hoặc không hợp lệ");

            if (!package.DurationDays.HasValue || package.DurationDays.Value <= 0)
                throw new Exception("Gói đăng ký không có thời hạn hợp lệ");

            if ((package.Price ?? 0) <= 0)
                throw new Exception("Gói miễn phí không hỗ trợ thanh toán qua VNPAY");

            var activeSub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (activeSub != null)
            {
                var isCurrentFreeDefault = activeSub.Package != null && activeSub.Package.IsDefault == true && (activeSub.Package.Price ?? 0) <= 0;
                if (!isCurrentFreeDefault && activeSub.Package != null)
                {
                    if ((activeSub.Package.DurationDays ?? 0) > (package.DurationDays ?? 0))
                        throw new Exception("Không thể chuyển xuống gói thấp hơn khi gói hiện tại còn hiệu lực. Vui lòng chờ hết hạn.");
                }
            }

            var now = DateTime.UtcNow;
            MemberSubscriptionPackage subscription;
            Transaction transaction;
            string orderId;

            await _unitOfWork.BeginTransactionAsync();
            try
            {

                subscription = new MemberSubscriptionPackage
                {
                    MemberId = member.Id,
                    PackageId = package.Id,
                    StartDate = now,
                    EndDate = now.AddDays(package.DurationDays.Value),
                    Status = MemberSubscriptionPackageStatus.INACTIVE.ToString()
                };
                await _unitOfWork.MemberSubscriptionPackages.AddAsync(subscription);
                await _unitOfWork.SaveChangesAsync();

                transaction = new Transaction
                {
                    UserId = userId,
                    DocNo = subscription.Id,
                    PaymentMethod = PaymentMethod.VNPAY.ToString(),
                    TransType = 3,
                    Description = $"Thanh toan goi dang ky {package.PackageName}",
                    Amount = package.Price.Value,
                    Currency = "VND",
                    Status = TransactionStatus.PENDING.ToString(),
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                orderId = $"CM_T_{IdEncoder.Encode(transaction.Id)}";

                var metadata = new VNPayTxMetadata
                {
                    OrderId = orderId
                };
                transaction.ExternalRefCode = JsonConverterUtil.Serialize(metadata);
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var nowVn = TimezoneUtil.ToVietNamTime(now);

            var vnpayParams = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(package.Price.Value * 100)).ToString() },
                { "vnp_CreateDate", nowVn.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", GetIpAddress() },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan goi dang ky {RemoveDiacritics(package.PackageName)}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _returnUrl },
                //{ "vnp_ExpireDate", nowVn.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                { "vnp_TxnRef", orderId }
            };

            var response = new VNPayLinkResponse
            {
                PayUrl = GenerateVnPayUrl(vnpayParams, _baseUrl, _hashSecret)
            };

            return response;
        }

        public async Task<VNPayLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) throw new Exception("Hồ sơ thành viên không tồn tại");

            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            if (wallet == null) throw new Exception("Ví của thành viên không tồn tại");

            if (request.Amount < 1000) throw new Exception("Số tiền nạp tối thiểu là 1.000 VND");

            var now = DateTime.UtcNow;
            Transaction transaction;
            string orderId;

            await _unitOfWork.BeginTransactionAsync();
            try
            {

                transaction = new Transaction
                {
                    UserId = userId,
                    DocNo = wallet.Id,
                    PaymentMethod = PaymentMethod.VNPAY.ToString(),
                    TransType = 4,
                    Description = "Nạp tiền vào ví qua VNPAY",
                    Amount = request.Amount,
                    Currency = "VND",
                    ExternalRefCode = null,
                    Status = TransactionStatus.PENDING.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                orderId = $"CM_T_{IdEncoder.Encode(transaction.Id)}";

                var metadata = new VNPayTxMetadata
                {
                    OrderId = orderId
                };
                transaction.ExternalRefCode = JsonConverterUtil.Serialize(metadata);
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var nowVn = TimezoneUtil.ToVietNamTime(now);

            var vnpayParams = new SortedList<string, string>(new VnPayCompare())
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(request.Amount * 100)).ToString() },
                { "vnp_CreateDate", nowVn.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", GetIpAddress() },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Nap tien vao vi Couplemood" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _returnUrl },
                { "vnp_TxnRef", orderId }
            };

            var response = new VNPayLinkResponse
            {
                PayUrl = GenerateVnPayUrl(vnpayParams, _baseUrl, _hashSecret)
            };

            return response;
        }

        public async Task<VNPayReturnDto?> VerifyPaymentProcessingAsync(IQueryCollection requestData)
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

            if (!myChecksum.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase))
            {
                return new VNPayReturnDto
                {
                    RspCode = "97",
                    Message = "Chữ ký không hợp lệ."
                };
            }

            // Get information
            var orderId = requestData["vnp_TxnRef"].ToString();
            var responseCode = vnpayData.GetValueOrDefault("vnp_ResponseCode");
            var transactionStatus = vnpayData.GetValueOrDefault("vnp_TransactionStatus");

            if (string.IsNullOrEmpty(orderId))
                {
                return new VNPayReturnDto
                {
                    RspCode = "01",
                    Message = "Không tìm thấy mã đơn hàng"
                };
            }

            var parts = orderId.Split('_');
            if (parts.Length < 3)
                {
                return new VNPayReturnDto
                {
                    RspCode = "01",
                    Message = "Mã đơn hàng không hợp lệ"
                };
            }
            var txId = IdEncoder.Decode(parts[2]);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tx = await _unitOfWork.Transactions.GetByIdAsync((int)txId);
                if (tx == null || tx.Status == TransactionStatus.SUCCESS.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return new VNPayReturnDto
                    {
                        RspCode = "02",
                        Message = "Giao dịch đã hoàn thành trước đó"
                    };
                }

                bool isSuccess = responseCode == "00" && transactionStatus == "00";

                if (isSuccess)
                {
                    tx.Status = TransactionStatus.SUCCESS.ToString();

                    // Update trans no to ref
                    var metadata = JsonConverterUtil.DeserializeOrDefault<VNPayTxMetadata>(tx.ExternalRefCode);
                    if (metadata != null)
                    {
                        metadata.TransactionNo = requestData["vnp_TransactionNo"].ToString();
                        tx.ExternalRefCode = JsonConverterUtil.Serialize(metadata);
                    }

                    if (tx.TransType == 3) // Subscription
                    {
                        var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
                        if (sub != null)
                        {
                            var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(sub.PackageId);
                            // Deactivate old sub
                            var currentActiveSub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(sub.MemberId);
                            if (currentActiveSub != null && currentActiveSub.Id != sub.Id)
                            {
                                currentActiveSub.Status = MemberSubscriptionPackageStatus.INACTIVE.ToString();
                                _unitOfWork.MemberSubscriptionPackages.Update(currentActiveSub);
                            }

                            sub.StartDate = DateTime.UtcNow;
                            sub.EndDate = DateTime.UtcNow.AddDays(package?.DurationDays ?? 0);
                            sub.Status = MemberSubscriptionPackageStatus.ACTIVE.ToString();
                            _unitOfWork.MemberSubscriptionPackages.Update(sub);
                        }
                    }
                    else if (tx.TransType == 4) // Wallet
                    {
                        var wallet = await _unitOfWork.Wallets.GetByIdAsync(tx.DocNo);
                        if (wallet != null)
                        {
                            wallet.Balance += tx.Amount;
                            _unitOfWork.Wallets.Update(wallet);
                        }
                    }
                }
                else
                {
                    tx.Status = TransactionStatus.FAILED.ToString();
                }

                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new VNPayReturnDto
                {
                    RspCode = "00",
                    Message = GetResponseMessage(responseCode)
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new VNPayReturnDto
                {
                    RspCode = "99",
                    Message = "Lỗi xử lý giao dịch"
                };
            }
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

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var result = sb.ToString().Normalize(NormalizationForm.FormC);

            result = result.Replace('đ', 'd').Replace('Đ', 'D');

            return result;
        }

        public async Task<TransactionResponse> CheckVNPAYTransactionStatusAsync(int userId, string orderId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var parts = orderId.Split('_');
            if (parts.Length < 3)
                throw new Exception("Mã giao dịch VNPAY (vnp_TxnRef) không hợp lệ");

            var transactionId = IdEncoder.Decode(parts[2]);

            var tx = await _unitOfWork.Transactions.GetByIdAsync((int)transactionId);
            if (tx == null || tx.UserId != userId)
                throw new Exception("Giao dịch không tồn tại hoặc không thuộc về người dùng");

            if (tx.TransType != 3 && tx.TransType != 4)
                throw new Exception("Giao dịch không hợp lệ (sai TransType)");

            var response = _mapper.Map<TransactionResponse>(tx);

            if (tx.TransType == 3)
            {
                var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
                if (sub == null)
                    throw new Exception("Không tìm thấy gói đăng ký của member");

                response.MemberSubscriptionId = tx.DocNo;
                response.StartDate = sub.StartDate;
                response.EndDate = sub.EndDate;
                response.IsActive = sub.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString();
            }

            return response;
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
