
using capstone_backend.Business.Common;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using static capstone_backend.Business.Services.ChallengeService;

namespace capstone_backend.Business.Jobs.Challenge
{
    public class ChallengeWorker : IChallengeWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChallengeWorker> _logger;
        private readonly IFcmService? _fcmService;

        public ChallengeWorker(IUnitOfWork unitOfWork, ILogger<ChallengeWorker> logger, IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fcmService = serviceProvider.GetService<IFcmService>();
        }

        public async Task AutoEndChallengeAsync()
        {
            var now = DateTime.UtcNow;
            var activeChallenges = await _unitOfWork.Challenges.GetAsync(c =>
                c.Status == ChallengeStatus.ACTIVE.ToString() &&
                c.EndDate <= now &&
                c.IsDeleted == false
            );

            int count = 0;
            foreach (var challenge in activeChallenges)
            {
                challenge.Status = ChallengeStatus.ENDED.ToString();
                challenge.UpdatedAt = now;
                _unitOfWork.Challenges.Update(challenge);
                count++;
            }
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Đã tự động kết thúc {count} Challenges.");
        }

        public async Task AutoInCompleteChallengeAsync()
        {
            var now = DateTime.UtcNow;
            var activeCoupleChallenges = await _unitOfWork.CoupleProfileChallenges.GetAsync(cpc =>
                cpc.Status == CoupleProfileChallengeStatus.IN_PROGRESS.ToString() &&
                cpc.Challenge.EndDate <= now &&
                cpc.IsDeleted == false,
                cpc => cpc.Include(cpc => cpc.Challenge)
            );

            int count = 0;
            foreach (var coupleChallenge in activeCoupleChallenges)
            {
                coupleChallenge.Status = CoupleProfileChallengeStatus.IN_COMPLETED.ToString();
                coupleChallenge.UpdatedAt = now;
                _unitOfWork.CoupleProfileChallenges.Update(coupleChallenge);
                count++;
            }
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Đã tự động chuyển {count} Couple Challenges sang trạng thái IN_COMPLETE.");
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

        public async Task SyncInactiveVenuesAsync()
        {
            var now = DateTime.UtcNow;
            int affectedCount = 0;

            var activeChallenge = await _unitOfWork.Challenges.GetAsync(c =>
                c.IsDeleted == false &&
                c.Status == ChallengeStatus.ACTIVE.ToString() &&
                c.ConditionRules != null
            );

            foreach (var challenge in activeChallenge)
            {
                try
                {
                    var challengePushNotis = new List<Data.Entities.Notification>();

                    var ruleWrapper = JsonConverterUtil.DeserializeOrDefault<ChallengeRuleWrapper>(challenge.ConditionRules);
                    var venueRule = ruleWrapper?.Rules?.FirstOrDefault(r => r.Key == ChallengeConstants.RuleKeys.VENUE_ID);

                    if (venueRule?.Value is not JsonElement valElement || valElement.ValueKind != JsonValueKind.Array)
                        continue;

                    var currentIds = JsonConverterUtil.DeserializeOrDefault<List<string>>(valElement.GetRawText()) ?? new();
                    if (!currentIds.Any()) 
                        continue;

                    var invalidVenues = await _unitOfWork.VenueLocations.GetInvalidVenueAsync(currentIds);
                    if (!invalidVenues.Any()) 
                        continue;

                    var invalidIds = invalidVenues.Select(v => v.Id).ToList();
                    var validIds = currentIds.Except(invalidIds).ToList();

                    venueRule.Value = validIds;
                    challenge.ConditionRules = JsonConverterUtil.Serialize(ruleWrapper);

                    if (challenge.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST)
                    {
                        challenge.TargetGoal = Math.Min(challenge.TargetGoal ?? 0, validIds.Count);
                        if (challenge.TargetGoal <= 0)
                            challenge.Status = ChallengeStatus.INACTIVE.ToString();
                    }

                    challenge.UpdatedAt = now;
                    _unitOfWork.Challenges.Update(challenge);
                    affectedCount++;

                    if (challenge.Status == ChallengeStatus.INACTIVE.ToString())
                    {
                        await _unitOfWork.SaveChangesAsync();
                        continue;
                    }

                    // --- Đồng bộ tiến độ cho Couple ---
                    var inProgressCCs = await _unitOfWork.CoupleProfileChallenges.GetAsync(cc =>
                        cc.ChallengeId == challenge.Id &&
                        cc.Status == CoupleProfileChallengeStatus.IN_PROGRESS.ToString(),
                        cc => cc.Include(cc => cc.Couple));

                    foreach (var cc in inProgressCCs)
                    {
                        var progressData = JsonConverterUtil.DeserializeOrDefault<CoupleChallengeProgressData>(cc.ProgressData);
                        if (progressData == null) 
                            continue;

                        bool isCoupleModified = false;

                        // A. Sync Target mới
                        if (progressData.Target != challenge.TargetGoal)
                        {
                            progressData.Target = challenge.TargetGoal ?? 0;
                            isCoupleModified = true;
                        }

                        if (challenge.GoalMetric == ChallengeConstants.GoalMetrics.UNIQUE_LIST && progressData.Unique?.Items != null)
                        {
                            var filteredUserItems = progressData.Unique.Items.Where(id => validIds.Contains(id)).ToList();
                            if (filteredUserItems.Count != progressData.Unique.Items.Count)
                            {
                                progressData.Unique.Items = filteredUserItems;
                                isCoupleModified = true;
                            }
                        }

                            // C. Hoàn thành sớm
                        if (cc.CurrentProgress > 0 && cc.CurrentProgress >= progressData.Target && progressData.Target > 0 && !progressData.IsCompleted)
                        {
                            cc.Status = CoupleProfileChallengeStatus.COMPLETED.ToString();
                            cc.CompletedAt = now;
                            progressData.IsCompleted = true;
                            isCoupleModified = true;

                            var couple = cc.Couple;
                            if (couple != null)
                            {
                                var m1 = await _unitOfWork.MembersProfile.GetByIdAsync(couple.MemberId1);
                                var m2 = await _unitOfWork.MembersProfile.GetByIdAsync(couple.MemberId2);

                                var validUserIds = new List<int>();

                                if (m1 != null && (m1.IsDeleted == false || m1.IsDeleted == null))
                                    validUserIds.Add(m1.UserId);

                                if (m2 != null && (m2.IsDeleted == false || m2.IsDeleted == null))
                                    validUserIds.Add(m2.UserId);

                                foreach (var uId in validUserIds)
                                {
                                    var noti = new Data.Entities.Notification
                                    {
                                        UserId = uId,
                                        Title = NotificationTemplate.Challenge.TitleCompleteChallengeSoon,
                                        Message = NotificationTemplate.Challenge.GetCompleteChallengeSoonBody(challenge.Title),
                                        Type = NotificationType.SYSTEM.ToString(),
                                        ReferenceId = challenge.Id,
                                        ReferenceType = ReferenceType.CHALLENGE.ToString(),
                                        CreatedAt = now,
                                        IsRead = false
                                    };

                                    await _unitOfWork.Notifications.AddAsync(noti);
                                    challengePushNotis.Add(noti);
                                }
                            }
                        }

                        if (isCoupleModified)
                        {
                            cc.ProgressData = JsonConverterUtil.Serialize(progressData);
                            cc.UpdatedAt = now;
                            _unitOfWork.CoupleProfileChallenges.Update(cc);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();

                    foreach (var n in challengePushNotis)
                    {
                        BackgroundJob.Enqueue<INotificationWorker>(j => j.SendPushNotificationAsync(n.Id));
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
    }
}
