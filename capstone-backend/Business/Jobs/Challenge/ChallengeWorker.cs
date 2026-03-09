
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;

namespace capstone_backend.Business.Jobs.Challenge
{
    public class ChallengeWorker : IChallengeWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChallengeWorker> _logger;

        public ChallengeWorker(IUnitOfWork unitOfWork, ILogger<ChallengeWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RenewDailyCheckinChallengesAsync()
        {
            var checkinChallenges = await _unitOfWork.Challenges.GetAsync(c =>
                c.TriggerEvent == ChallengeTriggerEvent.CHECKIN.ToString() &&
                c.IsDeleted == false
            );

            var checkinChallengeIds = checkinChallenges.Select(c => c.Id).ToList();
            if (!checkinChallengeIds.Any())
            {
                _logger.LogInformation("Không có Challenge CHECKIN nào để reset.");
                return;
            }

            var activeCouples = await _unitOfWork.CoupleProfiles.GetAsync(cp => cp.IsDeleted == false && cp.Status == "ACTIVE");
            var activeCoupleIds = activeCouples.Select(c => c.id).ToList();

            var coupleChallenges = await _unitOfWork.CoupleProfileChallenges.GetAsync(cpc =>
                checkinChallengeIds.Contains(cpc.ChallengeId) &&
                activeCoupleIds.Contains(cpc.CoupleId) &&
                cpc.IsDeleted == false
            );

            int count = 0;
            foreach (var coupleChallenge in coupleChallenges)
            {
                if (string.IsNullOrEmpty(coupleChallenge.ProgressData))
                    continue;

                var progress = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(coupleChallenge.ProgressData);

                if (progress?.Streak != null && progress.Streak.Mode == "DAILY")
                {
                    coupleChallenge.CurrentProgress = 0;
                    coupleChallenge.Status = CoupleProfileChallengeStatus.IN_PROGRESS.ToString();

                    progress.Current = 0;
                    progress.IsCompleted = false;

                    coupleChallenge.ProgressData = JsonConverterUtil.Serialize(progress);
                    coupleChallenge.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.CoupleProfileChallenges.Update(coupleChallenge);
                    count++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Đã reset thành công {count} Couple Challenges.");
        }
    }
}
