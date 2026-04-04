using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.Payment
{
    public class PaymentWorker : IPaymentWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentWorker> _logger;

        public PaymentWorker(IUnitOfWork unitOfWork, ILogger<PaymentWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [JobDisplayName("Auto Expire Pending Payments")]
        public async Task AutoExpirePendingPaymentsAsync()
        {
            var thresholdTime = DateTime.UtcNow.AddMinutes(-5);

            _logger.LogInformation("[AUTO EXPIRE] Checking for pending payments older than {ThresholdTime}", thresholdTime);

            var expiredTransactions = await _unitOfWork.Context.Set<Transaction>()
                .Where(t => t.Status == TransactionStatus.PENDING.ToString()
                    && t.CreatedAt < thresholdTime)
                .ToListAsync();

            if (!expiredTransactions.Any())
            {
                _logger.LogInformation("[AUTO EXPIRE] No expired pending payments found.");
                return;
            }

            _logger.LogInformation("[AUTO EXPIRE] Found {Count} expired pending payment(s)", expiredTransactions.Count);

            foreach (var transaction in expiredTransactions)
            {
                try
                {
                    transaction.Status = TransactionStatus.EXPIRED.ToString();
                    transaction.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Context.Set<Transaction>().Update(transaction);

                    // Handle VENUE_SUBSCRIPTION (TransType = 1)
                    if (transaction.TransType == (int)TransactionType.VENUE_SUBSCRIPTION)
                    {
                        var subscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                            .FirstOrDefaultAsync(s => s.Id == transaction.DocNo
                                && s.Status == VenueSubscriptionPackageStatus.PENDING_PAYMENT.ToString());

                        if (subscription != null)
                        {
                            subscription.Status = VenueSubscriptionPackageStatus.PAYMENT_FAILED.ToString();
                            subscription.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                        }
                    }
                    // Handle ADS_ORDER (TransType = 2)
                    else if (transaction.TransType == (int)TransactionType.ADS_ORDER)
                    {
                        var adsOrder = await _unitOfWork.Context.Set<AdsOrder>()
                            .FirstOrDefaultAsync(ao => ao.Id == transaction.DocNo
                                && ao.Status == AdsOrderStatus.PENDING.ToString());

                        if (adsOrder != null)
                        {
                            adsOrder.Status = AdsOrderStatus.PAYMENT_FAILED.ToString();
                            adsOrder.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Context.Set<AdsOrder>().Update(adsOrder);

                            var venueAds = await _unitOfWork.Context.Set<VenueLocationAdvertisement>()
                                .Where(vla => vla.AdvertisementId == adsOrder.AdvertisementId
                                    && vla.Status == VenueLocationAdvertisementStatus.PENDING_PAYMENT.ToString())
                                .ToListAsync();

                            foreach (var vla in venueAds)
                            {
                                vla.Status = VenueLocationAdvertisementStatus.CANCELLED.ToString();
                                vla.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.Context.Set<VenueLocationAdvertisement>().Update(vla);
                            }
                        }
                    }
                    // Handle MEMBER_SUBSCRIPTION (TransType = 3) - MoMo payment
                    else if (transaction.TransType == (int)TransactionType.MEMBER_SUBSCRIPTION)
                    {
                        var memberSub = await _unitOfWork.MemberSubscriptionPackages.GetFirstAsync(ms => ms.Id == transaction.DocNo && ms.Status == MemberSubscriptionPackageStatus.INACTIVE.ToString());

                        if (memberSub != null)
                        {
                            memberSub.Status = MemberSubscriptionPackageStatus.CANCELLED.ToString();
                            memberSub.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.MemberSubscriptionPackages.Update(memberSub);
                        }
                        _logger.LogInformation("[AUTO EXPIRE] Expired MEMBER_SUBSCRIPTION Transaction #{TxId}", transaction.Id);
                    }
                    // Handle WALLET_TOPUP (TransType = 4) - MoMo payment
                    else if (transaction.TransType == (int)TransactionType.WALLET_TOPUP)
                    {
                        // Wallet topup via MoMo - just expire transaction
                        // Wallet balance is not affected since payment was not completed
                        _logger.LogInformation("[AUTO EXPIRE] Expired WALLET_TOPUP Transaction #{TxId}", transaction.Id);
                    }

                    _logger.LogInformation("[AUTO EXPIRE] ✅ Expired Transaction #{TxId} (TransType: {TransType})",
                        transaction.Id, transaction.TransType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AUTO EXPIRE] ❌ Error expiring Transaction #{TxId}", transaction.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("[AUTO EXPIRE] ✅ Completed expiring {Count} payment(s)", expiredTransactions.Count);
        }
    }
}
