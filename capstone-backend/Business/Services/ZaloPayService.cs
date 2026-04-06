using Amazon.Rekognition.Model;
using AutoMapper;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.DTOs.Zalo;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using System.Security.Cryptography;
using System.Text;

namespace capstone_backend.Business.Services
{
    public class ZaloPayService : IZaloPayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;

        private readonly string _endpoint = Environment.GetEnvironmentVariable("ZALOPAY_ENDPOINT");
        private readonly string _appId = Environment.GetEnvironmentVariable("ZALOPAY_APP_ID");
        private readonly string _key1 = Environment.GetEnvironmentVariable("ZALOPAY_APP_KEY1");
        private readonly string _key2 = Environment.GetEnvironmentVariable("ZALOPAY_APP_KEY2");
        private readonly string _callbackUrl = Environment.GetEnvironmentVariable("ZALOPAY_CALLBACK_URL");
        private readonly string _redirectUrl = Environment.GetEnvironmentVariable("PAYMENT_REDIRECT_URL");

        public ZaloPayService(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
        }

        public async Task<ZaloPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) throw new Exception("Hồ sơ thành viên không tồn tại");

            if (request.PaymentMethod != PaymentMethod.ZALOPAY.ToString())
                throw new Exception("Phương thức thanh toán không hợp lệ");

            var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(request.PackageId);
            if (package == null || package.IsDeleted == true || package.IsActive == false)
                throw new Exception("Gói đăng ký không tồn tại hoặc không hợp lệ");

            if (!package.DurationDays.HasValue || package.DurationDays.Value <= 0)
                throw new Exception("Gói đăng ký không có thời hạn hợp lệ");

            if ((package.Price ?? 0) <= 0)
                throw new Exception("Gói miễn phí không hỗ trợ thanh toán qua ZaloPay");

            var activeSub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);

            if (activeSub != null)
            {
                var isCurrentFreeDefault = activeSub.Package != null && activeSub.Package.IsDefault == true && (activeSub.Package.Price ?? 0) <= 0;
                if (!isCurrentFreeDefault)
                {
                    if ((activeSub.Package?.DurationDays ?? 0) > (package.DurationDays ?? 0))
                    {
                        throw new Exception("Không thể chuyển xuống gói thấp hơn khi gói hiện tại còn hiệu lực. Vui lòng chờ hết hạn.");
                    }
                }
            }

            var now = DateTime.UtcNow;
            MemberSubscriptionPackage subscription;
            Transaction transaction;

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
                    PaymentMethod = PaymentMethod.ZALOPAY.ToString(),
                    TransType = 3,
                    Description = $"Thanh toán gói đăng ký {package.PackageName} qua ZaloPay",
                    Amount = package.Price.Value,
                    Currency = "VND",
                    ExternalRefCode = null,
                    Status = TransactionStatus.PENDING.ToString(),
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var nowVn = TimezoneUtil.ToVietNamTime(now);

            var appTransId = $"{nowVn:yyMMdd}_{IdEncoder.Encode(transaction.Id)}";
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var amount = (long)package.Price.Value;

            var embedData = new Dictionary<string, string>
            {
                ["redirecturl"] = _redirectUrl,
                ["business"] = "member_subscription",
                ["packageId"] = package.Id.ToString(),
                ["subscriptionId"] = subscription.Id.ToString()
            };

            var items = new[] { new { itemid = package.Id.ToString(), itemname = package.PackageName, itemprice = amount, itemquantity = 1 } };

            var embedDataStr = JsonConverterUtil.Serialize(embedData);
            var itemStr = JsonConverterUtil.Serialize(items);

            var rawMac = $"{_appId}|{appTransId}|{userId}|{amount}|{appTime}|{embedDataStr}|{itemStr}";
            var mac = GetHmacSha256(rawMac, _key1);

            var zaloRequest = new
            {
                app_id = int.Parse(_appId),
                app_user = userId.ToString(),
                app_trans_id = appTransId,
                app_time = appTime,
                expire_duration_seconds = 15 * 60,
                amount = amount,
                item = itemStr,
                description = $"Thanh toán gói {package.PackageName}",
                embed_data = embedDataStr,
                bank_code = "",
                mac = mac,
                callback_url = _callbackUrl
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(_endpoint, zaloRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonConverterUtil.DeserializeOrDefault<ZaloPayLinkResponse>(responseText);

                if (!response.IsSuccessStatusCode || result == null || result.ReturnCode != 1)
                {
                    throw new Exception($"Lỗi từ ZaloPay code: {result.ReturnCode}: {result?.ReturnMessage ?? responseText}");
                }

                var tx = await _unitOfWork.Transactions.GetByIdAsync(transaction.Id);
                tx!.ExternalRefCode = JsonConverterUtil.Serialize(new ZaloPayTxMetadata { AppTransId = appTransId, OrderUrl = result.OrderUrl });
                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch
            {
                await MarkTransactionFailedAsync(transaction.Id);
                throw;
            }
        }

        public async Task<ZaloPayLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) throw new Exception("Hồ sơ thành viên không tồn tại");

            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            if (wallet == null) throw new Exception("Ví của thành viên không tồn tại");

            if (request.Amount < 1000) throw new Exception("Số tiền nạp tối thiểu là 1.000 VND");

            var now = DateTime.UtcNow;
            Transaction transaction;

            await _unitOfWork.BeginTransactionAsync();
            try
            {

                transaction = new Transaction
                {
                    UserId = userId,
                    DocNo = wallet.Id,
                    PaymentMethod = PaymentMethod.ZALOPAY.ToString(),
                    TransType = 4,
                    Description = "Nạp tiền vào ví qua ZaloPay",
                    Amount = request.Amount,
                    Currency = "VND",
                    ExternalRefCode = null,
                    Status = TransactionStatus.PENDING.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var appTransId = $"{now:yyMMdd}_{IdEncoder.Encode(transaction.Id)}";
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var amount = (long)request.Amount;

            var embedData = new Dictionary<string, string>
            {
                ["redirecturl"] = _redirectUrl,
                ["business"] = "wallet_topup",
                ["userId"] = userId.ToString(),
                ["walletId"] = wallet.Id.ToString()
            };

            var items = new[] { new { itemid = wallet.Id.ToString(), itemname = "Nạp tiền ví", itemprice = amount, itemquantity = 1 } };

            var embedDataStr = JsonConverterUtil.Serialize(embedData);
            var itemStr = JsonConverterUtil.Serialize(items);

            var rawMac = $"{_appId}|{appTransId}|{userId}|{amount}|{appTime}|{embedDataStr}|{itemStr}";
            var mac = GetHmacSha256(rawMac, _key1);

            var zaloRequest = new
            {
                app_id = int.Parse(_appId),
                app_user = userId.ToString(),
                app_trans_id = appTransId,
                app_time = appTime,
                expire_duration_seconds = 15 * 60,
                amount = amount,
                item = itemStr,
                description = $"Couplemood - Nạp {amount} VND",
                embed_data = embedDataStr,
                bank_code = "",
                mac = mac,
                callback_url = _callbackUrl
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(_endpoint, zaloRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonConverterUtil.DeserializeOrDefault<ZaloPayLinkResponse>(responseText);

                if (!response.IsSuccessStatusCode || result == null || result.ReturnCode != 1)
                {
                    throw new Exception($"Lỗi từ ZaloPay: {result?.ReturnMessage ?? responseText}");
                }

                var tx = await _unitOfWork.Transactions.GetByIdAsync(transaction.Id);
                tx!.ExternalRefCode = JsonConverterUtil.Serialize(new ZaloPayTxMetadata { AppTransId = appTransId, OrderUrl = result.OrderUrl });
                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();

                return result;
            }
            catch
            {
                await MarkTransactionFailedAsync(transaction.Id);
                throw;
            }
        }

        private async Task MarkTransactionFailedAsync(int transactionId)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId);
            if (transaction == null || transaction.Status == TransactionStatus.SUCCESS.ToString()) return;

            if (transaction.TransType == 3)
            {
                var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(transaction.DocNo);
                if (sub != null)
                {
                    sub.Status = MemberSubscriptionPackageStatus.CANCELLED.ToString();
                    _unitOfWork.MemberSubscriptionPackages.Update(sub);
                }
            }

            transaction.Status = TransactionStatus.FAILED.ToString();
            _unitOfWork.Transactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> VerifyPaymentProcessing(ZaloPayCallbackRequest request)
        {
            var computedMac = GetHmacSha256(request.Data, _key2);
            if (!string.Equals(computedMac, request.Mac, StringComparison.OrdinalIgnoreCase)) return false;

            var cbData = JsonConverterUtil.DeserializeOrDefault<ZaloPayCallbackData>(request.Data);
            if (cbData == null || string.IsNullOrWhiteSpace(cbData.AppTransId)) 
                return false;

            var parts = cbData.AppTransId.Split('_');
            if (parts.Length < 2) 
                return false;

            var txId = IdEncoder.Decode(parts[1]);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tx = await _unitOfWork.Transactions.GetByIdAsync((int)txId);
                if (tx == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                if (tx.Status == TransactionStatus.SUCCESS.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return true;
                }

                if (tx.Status == TransactionStatus.FAILED.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return true;
                }

                tx.Status = TransactionStatus.SUCCESS.ToString();

                var metadata = JsonConverterUtil.DeserializeOrDefault<ZaloPayTxMetadata>(tx.ExternalRefCode);
                if (metadata != null)
                {
                    metadata.ZpTransId = cbData.ZpTransId;
                    tx.ExternalRefCode = JsonConverterUtil.Serialize(metadata);
                }

                if (tx.TransType == 3)
                {
                    var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
                    if (sub != null)
                    {
                        var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(sub.PackageId);
                        var realNow = DateTime.UtcNow;

                        var currentActiveSub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(sub.MemberId);
                        if (currentActiveSub != null && currentActiveSub.Id != sub.Id)
                        {
                            currentActiveSub.Status = MemberSubscriptionPackageStatus.INACTIVE.ToString();
                            currentActiveSub.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.MemberSubscriptionPackages.Update(currentActiveSub);
                        }

                        sub.StartDate = realNow;
                        sub.EndDate = realNow.AddDays(package?.DurationDays ?? 0);
                        sub.Status = MemberSubscriptionPackageStatus.ACTIVE.ToString();
                        _unitOfWork.MemberSubscriptionPackages.Update(sub);
                    }
                }
                else if (tx.TransType == 4)
                {
                    var wallet = await _unitOfWork.Wallets.GetByIdAsync(tx.DocNo);
                    if (wallet != null)
                    {
                        wallet.Balance += tx.Amount;
                        _unitOfWork.Wallets.Update(wallet);
                    }
                }

                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
        }

        private static string GetHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            return BitConverter.ToString(hmac.ComputeHash(messageBytes)).Replace("-", "").ToLower();
        }

        public async Task<TransactionResponse> CheckZaloTransactionStatusAsync(int userId, string appTransId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            // ZaloPay app_trans_id format: yyMMdd_encodedId
            var parts = appTransId.Split("_");
            if (parts.Length < 2)
                throw new Exception("Mã giao dịch ZaloPay (app_trans_id) không hợp lệ");

            var transactionId = IdEncoder.Decode(parts[1]);

            var tx = await _unitOfWork.Transactions.GetByIdAsync((int)transactionId);
            if (tx == null || tx.UserId != userId)
                throw new Exception("Giao dịch không tồn tại hoặc không thuộc về người dùng");

            if (tx.TransType != 3 && tx.TransType != 4)
                throw new Exception("Giao dịch không hợp lệ (sai TransType)");

            var response = _mapper.Map<TransactionResponse>(tx);

            // Xử lý riêng cho Member Subscription
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

            var metadata = JsonConverterUtil.DeserializeOrDefault<ZaloPayTxMetadata>(tx.ExternalRefCode);

            response.PayUrl = metadata?.OrderUrl;

            return response;
        }
    }
}
