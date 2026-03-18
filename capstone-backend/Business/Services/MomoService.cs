using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Npgsql.Replication.PgOutput.Messages;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

            var now = DateTime.UtcNow;

            MemberSubscriptionPackage subscription;
            Transaction transaction;

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
                transaction.ExternalRefCode = $"CM_R_{transaction.Id}_{seed}";
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var orderId = $"CM_T_{IdEncoder.Encode(transaction.Id)}";
            var requestId = transaction.ExternalRefCode!;

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

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(transaction.DocNo);
            if (sub != null)
            {
                sub.Status = MemberSubscriptionPackageStatus.CANCELED.ToString();
                _unitOfWork.MemberSubscriptionPackages.Update(sub);
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
            // 1. Verify result
            if (string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.PartnerCode))
                return false;

            if (request.PartnerCode != _partnerCode)
                return false;

            if (request.Amount <= 0)
                return false;

            // 2. Verify signature
            var rawdata = $"accessKey={_accessKey}" +
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

            // 3. Verify request with database
            var parts = request.OrderId!.Split('_');
            if (parts.Length < 3)
                return false;

            var realEncodedId = parts[2];
            var txId = IdEncoder.Decode(realEncodedId);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tx = await _unitOfWork.Transactions.GetByIdAsync((int)txId);
                if (tx == null)
                    return false;

                if (tx.Status == TransactionStatus.SUCCESS.ToString() ||
                    (tx.Status == TransactionStatus.FAILED.ToString() && request.ResultCode != 0))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                if (request.ResultCode == 0)
                {
                    tx.Status = TransactionStatus.SUCCESS.ToString();
                    tx.ExternalRefCode = request.TransId.ToString();

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
                else
                {
                    tx.Status = TransactionStatus.FAILED.ToString();
                }

                _unitOfWork.Transactions.Update(tx);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }

            // 4. Return true if processing is successful, false otherwise

            return true;
        }

        private static bool VerifySignature(string rawData, string secretKey, string providedSignature)
        {
            var computedSignature = GetSignature(rawData, secretKey);

            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

            if (computedBytes.Length!= providedBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
        }
    }
}
