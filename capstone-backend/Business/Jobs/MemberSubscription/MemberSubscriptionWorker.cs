
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Jobs.MemberSubscription
{
    public class MemberSubscriptionWorker : IMemberSubscriptionWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MemberSubscriptionWorker> _logger;

        public MemberSubscriptionWorker(IUnitOfWork unitOfWork, ILogger<MemberSubscriptionWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task AutoExpireMemberSubscriptionAsync()
        {
            var now = DateTime.UtcNow;

            _logger.LogInformation("[AUTO EXPIRE MEMBER SUB] Starting auto-expire member subscriptions job at {Time}", now);

            var expiredSubscriptions = await _unitOfWork.MemberSubscriptionPackages.GetExpiredSubscriptionsAsync(now);
            
            if (!expiredSubscriptions.Any())
            {
                _logger.LogInformation("[AUTO EXPIRE MEMBER SUB] No expired active member subscriptions found.");
                return;
            }

            foreach (var sub in expiredSubscriptions)
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // re-check to avoud race condition
                    var currentSub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(sub.Id);
                    if (currentSub == null ||
                        currentSub.Status != MemberSubscriptionPackageStatus.ACTIVE.ToString() ||
                        currentSub.EndDate == null ||
                        currentSub.EndDate > now)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        continue;
                    }

                    currentSub.Status = MemberSubscriptionPackageStatus.EXPIRED.ToString();
                    currentSub.UpdatedAt = now;

                    _unitOfWork.MemberSubscriptionPackages.Update(currentSub);

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("[AUTO EXPIRE MEMBER SUB] Successfully auto-expired member subscription with ID {SubscriptionId} for member {MemberId}", currentSub.Id, currentSub.MemberId);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "[AUTO EXPIRE MEMBER SUB] Error auto-expiring member subscription with ID {SubscriptionId}", sub.Id);
                }
            }

            _logger.LogInformation("[AUTO EXPIRE MEMBER SUB] Completed auto-expire member subscriptions job at {Time}", DateTime.UtcNow);
        }
    }
}
