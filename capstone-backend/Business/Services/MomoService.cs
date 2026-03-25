using Amazon.Rekognition.Model;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using System.Security.Cryptography;
using System.Text;

namespace capstone_backend.Business.Services
{
    public class MomoService : IMomoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _endpoint = Environment.GetEnvironmentVariable("MOMO_ENDPOINT");
        private readonly string _partnerCode = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE");
        private readonly string _accessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY");
        private readonly string _secretKey = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY");
        private readonly string _ipnUrl = Environment.GetEnvironmentVariable("MOMO_IPN_URL");

        public MomoService(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<MomoLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(request.PackageId);
            if (package == null || package.IsDeleted == true || package.IsActive == false)
                throw new Exception("Gói đăng ký không tồn tại hoặc không hợp lệ");

            var activeSub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (activeSub != null)
                throw new Exception("Bạn đã có gói đăng ký đang hoạt động. Vui lòng chờ đến khi gói hiện tại hết hạn hoặc hủy bỏ trước khi mua gói mới.");

            var now = DateTime.UtcNow;

            MemberSubscriptionPackage subscription;
            Transaction transaction;
            string orderId;
            string requestId;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                Transaction? recentTransaction = await _unitOfWork.Transactions.GetRecentPendingAsync(userId, request.PackageId, now.AddMinutes(-30));

                if (recentTransaction != null)
                {
                    recentTransaction.Status = TransactionStatus.CANCELLED.ToString();
                    _unitOfWork.Transactions.Update(recentTransaction);
                }

                // 1. Create subscription record
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

                // 2. Create transaction record
                transaction = new Transaction
                {
                    UserId = userId,
                    DocNo = subscription.Id,
                    PaymentMethod = PaymentMethod.MOMO.ToString(),
                    TransType = 3,
                    Description = $"Thanh toán gói đăng ký {package.PackageName} qua MoMo",
                    Amount = package.Price.Value,
                    Currency = "VND",
                    ExternalRefCode = null,
                    Status = TransactionStatus.PENDING.ToString(),
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                // 3. Generate MoMo payment link
                var seed = Guid.NewGuid().ToString("N")[..6];
                orderId = $"CM_T_{IdEncoder.Encode(transaction.Id)}";
                requestId = $"CM_R_{transaction.Id}_{seed}";

                var metadata = new MomoTransactionMetadata
                {
                    RequestId = requestId,
                    OrderId = orderId
                };

                transaction.ExternalRefCode = ToMomoMetadataJson(metadata);
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var momoRequest = new CreateMomoPaymentRequest
            {
                PartnerCode = _partnerCode,
                StoreName = "Couplemood",
                RequestId = requestId,
                Amount = (long)package.Price!.Value,
                OrderId = orderId,
                OrderInfo = $"Thanh toán gói {package.PackageName} qua MoMo",
                RedirectUrl = Environment.GetEnvironmentVariable("PAYMENT_REDIRECT_URL"),
                IpnUrl = _ipnUrl,
                RequestType = "captureWallet",
                ExtraData = "",
                Items = new List<PaymentItems>
                {
                    new PaymentItems
                    {
                        Id = package.Id.ToString(),
                        Name = package.PackageName,
                        Description = package.Description,
                        Price = (long)package.Price.Value,
                        Manufacturer = "Couplemood",
                        Quantity = 1,
                        Currency = "VND",
                        Unit = "gói",
                        TaxAmount = 0
                    }
                },
                UserInfo = new UserInfo
                {
                    Name = member.FullName,
                    Email = member.User.Email,
                    PhoneNumber = member.User.PhoneNumber
                },
                Lang = "vi",
                Signature = ""
            };

            var rawSignature =
                $"accessKey={_accessKey}" +
                $"&amount={momoRequest.Amount}" +
                $"&extraData=" +
                $"&ipnUrl={_ipnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={momoRequest.OrderInfo}" +
                $"&partnerCode={_partnerCode}" +
                $"&redirectUrl={momoRequest.RedirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={momoRequest.RequestType}";

            momoRequest.Signature = GetSignature(rawSignature, _secretKey);

            CreateMomoPaymenResponse? result = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(_endpoint, momoRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                result = JsonConverterUtil.DeserializeOrDefault<CreateMomoPaymenResponse>(responseText);

                if (!response.IsSuccessStatusCode || result == null || string.IsNullOrWhiteSpace(result.PayUrl))
                {
                    throw new Exception($"Lỗi từ MoMo: {result?.Message ?? responseText}");
                }

                var tx = await _unitOfWork.Transactions.GetByIdAsync(transaction.Id);
                if (tx == null)
                    throw new Exception("Không tìm thấy transaction sau khi tạo");

                var metadata = GetMomoMetadata(tx.ExternalRefCode);
                metadata.PayUrl = result.PayUrl;
                metadata.DeepLink = result.DeepLink;
                metadata.QrCodeUrl = result.QrCodeUrl;
                metadata.DeeplinkMiniApp = result.DeeplinkMiniApp;

                tx.ExternalRefCode = ToMomoMetadataJson(metadata);
                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();

                return new MomoLinkResponse
                {
                    PayUrl = result.PayUrl,
                    DeepLink = result.DeepLink,
                    QrCodeUrl = result.QrCodeUrl,
                    DeeplinkMiniApp = result.DeeplinkMiniApp
                };
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
            if (transaction == null)
                return;

            if (transaction.Status == TransactionStatus.SUCCESS.ToString())
                return;

            if (transaction.TransType == 3)
            {
                var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(transaction.DocNo);
                if (sub != null)
                {
                    sub.Status = MemberSubscriptionPackageStatus.CANCELED.ToString();
                    _unitOfWork.MemberSubscriptionPackages.Update(sub);
                }
            }

            transaction.Status = TransactionStatus.FAILED.ToString();
            _unitOfWork.Transactions.Update(transaction);
            await _unitOfWork.SaveChangesAsync();
        }

        private static string GetSignature(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            return BitConverter.ToString(hmac.ComputeHash(messageBytes)).Replace("-", "").ToLower();
        }

        public async Task<bool> VerifyPaymentProcessing(MomoIpnRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderId) ||
                string.IsNullOrWhiteSpace(request.RequestId) ||
                string.IsNullOrWhiteSpace(request.PartnerCode))
                return false;

            if (request.PartnerCode != _partnerCode)
                return false;

            if (request.Amount <= 0)
                return false;

            var rawdata =
                $"accessKey={_accessKey}" +
                $"&amount={request.Amount}" +
                $"&extraData={request.ExtraData}" +
                $"&message={request.Message}" +
                $"&orderId={request.OrderId}" +
                $"&orderInfo={request.OrderInfo}" +
                $"&orderType={request.OrderType}" +
                $"&partnerCode={request.PartnerCode}" +
                $"&payType={request.PayType}" +
                $"&requestId={request.RequestId}" +
                $"&responseTime={request.ResponseTime}" +
                $"&resultCode={request.ResultCode}" +
                $"&transId={request.TransId}";

            if (!VerifySignature(rawdata, _secretKey, request.Signature!))
                return false;

            var parts = request.OrderId.Split('_');
            if (parts.Length < 3)
                return false;

            var realEncodedId = parts[2];
            var txId = IdEncoder.Decode(realEncodedId);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tx = await _unitOfWork.Transactions.GetByIdAsync((int)txId);
                if (tx == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                var metadata = GetMomoMetadata(tx.ExternalRefCode);

                if (!string.Equals(metadata.OrderId, request.OrderId, StringComparison.Ordinal) ||
                    !string.Equals(metadata.RequestId, request.RequestId, StringComparison.Ordinal))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                if (tx.Status == TransactionStatus.SUCCESS.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return true;
                }

                if (tx.Status == TransactionStatus.FAILED.ToString() && request.ResultCode != 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return true;
                }

                if (request.ResultCode == 0)
                {
                    tx.Status = TransactionStatus.SUCCESS.ToString();

                    metadata.TransId = request.TransId;
                    tx.ExternalRefCode = ToMomoMetadataJson(metadata);

                    if (tx.TransType == 3)
                    {
                        var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
                        if (sub != null)
                        {
                            var package = await _unitOfWork.SubscriptionPackages.GetByIdAsync(sub.PackageId);
                            var realNow = DateTime.UtcNow;

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
                }
                else
                {
                    tx.Status = TransactionStatus.FAILED.ToString();
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

        private static bool VerifySignature(string rawData, string secretKey, string providedSignature)
        {
            var computedSignature = GetSignature(rawData, secretKey);

            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

            if (computedBytes.Length != providedBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
        }

        private static MomoTransactionMetadata GetMomoMetadata(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new MomoTransactionMetadata();

            return JsonConverterUtil.DeserializeOrDefault<MomoTransactionMetadata>(json)
                   ?? new MomoTransactionMetadata();
        }

        private static string ToMomoMetadataJson(MomoTransactionMetadata metadata)
        {
            return JsonConverterUtil.Serialize(metadata);
        }

        public async Task<MomoLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
            if (wallet == null)
                throw new Exception("Ví của thành viên không tồn tại");

            if (request.Amount < 1000)
                throw new Exception("Số tiền nạp tối thiểu là 1.000 VND");

            string orderId;
            string requestId;
            var now = DateTime.UtcNow;

            Transaction? transaction;
            var metadata = new MomoTransactionMetadata();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Cancel any pending wallet top-up transactions in the last 30 minutes
                Transaction? recentTransaction = await _unitOfWork.Transactions.GetWalletTopupPendingAsync(userId, now.AddMinutes(-30));
                if (recentTransaction != null)
                {
                    recentTransaction.Status = TransactionStatus.CANCELLED.ToString();
                    _unitOfWork.Transactions.Update(recentTransaction);
                    await _unitOfWork.SaveChangesAsync();
                }

                transaction = new Transaction
                {
                    UserId = userId,
                    DocNo = wallet.Id,
                    PaymentMethod = PaymentMethod.MOMO.ToString(),
                    TransType = 4,
                    Description = $"Nạp tiền vào ví qua MoMo",
                    Amount = request.Amount,
                    Currency = "VND",
                    ExternalRefCode = null,
                    Status = TransactionStatus.PENDING.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                var seed = Guid.NewGuid().ToString("N")[..6];
                orderId = $"CM_T_{IdEncoder.Encode(transaction.Id)}";
                requestId = $"CM_R_{transaction.Id}_{seed}";

                metadata = new MomoTransactionMetadata
                {
                    RequestId = requestId,
                    OrderId = orderId
                };

                transaction.ExternalRefCode = ToMomoMetadataJson(metadata);
                _unitOfWork.Transactions.Update(transaction);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var extraData = new Dictionary<string, string>()
            {
                ["business"] = "wallet_topup",
                ["userId"] = userId.ToString(),
                ["walletId"] = wallet.Id.ToString()
            };

            var momoRequest = new CreateMomoPaymentRequest
            {
                PartnerCode = _partnerCode,
                StoreName = "Couplemood",
                RequestId = requestId,
                Amount = (long)request.Amount,
                OrderId = orderId,
                OrderInfo = $"Nạp tiền vào ví qua MoMo",
                RedirectUrl = Environment.GetEnvironmentVariable("PAYMENT_REDIRECT_URL"),
                IpnUrl = _ipnUrl,
                RequestType = "captureWallet",
                ExtraData = EncodeBase64(extraData),
                Items = new List<PaymentItems>
                {
                    new PaymentItems
                    {
                        Id = wallet.Id.ToString(),
                        Name = "Nạp tiền vào ví",
                        Description = $"Nạp {request.Amount} VND vào ví Couplemood",
                        Price = (long)request.Amount,
                        Manufacturer = "Couplemood",
                        Quantity = 1,
                        Currency = "VND",
                        Unit = "lần",
                        TaxAmount = 0
                    }
                },
                UserInfo = new UserInfo
                {
                    Name = member.FullName,
                    Email = member.User.Email,
                    PhoneNumber = member.User.PhoneNumber
                },
                Lang = "vi",
                Signature = ""
            };

            var rawSignature =
                $"accessKey={_accessKey}" +
                $"&amount={momoRequest.Amount}" +
                $"&extraData={momoRequest.ExtraData}" +
                $"&ipnUrl={_ipnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={momoRequest.OrderInfo}" +
                $"&partnerCode={_partnerCode}" +
                $"&redirectUrl={momoRequest.RedirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={momoRequest.RequestType}";

            momoRequest.Signature = GetSignature(rawSignature, _secretKey);

            CreateMomoPaymenResponse? result = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(_endpoint, momoRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                result = JsonConverterUtil.DeserializeOrDefault<CreateMomoPaymenResponse>(responseText);

                if (!response.IsSuccessStatusCode || result == null || string.IsNullOrWhiteSpace(result.PayUrl))
                {
                    throw new Exception($"Lỗi từ MoMo: {result?.Message ?? responseText}");
                }

                var tx = await _unitOfWork.Transactions.GetByIdAsync(transaction.Id);
                if (tx == null)
                    throw new Exception("Không tìm thấy transaction sau khi tạo");

                metadata.PayUrl = result.PayUrl;
                metadata.DeepLink = result.DeepLink;
                metadata.QrCodeUrl = result.QrCodeUrl;
                metadata.DeeplinkMiniApp = result.DeeplinkMiniApp;

                tx.ExternalRefCode = ToMomoMetadataJson(metadata);
                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();

                return new MomoLinkResponse
                {
                    PayUrl = result.PayUrl,
                    DeepLink = result.DeepLink,
                    QrCodeUrl = result.QrCodeUrl,
                    DeeplinkMiniApp = result.DeeplinkMiniApp
                };
            }
            catch
            {
                await MarkTransactionFailedAsync(transaction.Id);
                throw;
            }
        }

        private static string EncodeBase64(Dictionary<string, string> data)
        {
            var json = JsonConverterUtil.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}
