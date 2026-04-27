
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;

namespace capstone_backend.Business.Jobs.MemberAccessory
{
    public class MemberAccessoryWorker : IMemberAccessoryWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MemberAccessoryWorker> _logger;
        private const int TOP_1_KING_ACCESSORY_ID = 10;
        private const int TOP_1_QUEEN_ACCESSORY_ID = 11;

        public MemberAccessoryWorker(IUnitOfWork unitOfWork, ILogger<MemberAccessoryWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RewardTop1MonthlyAsync()
        {
            var now = DateTime.UtcNow;
            var target = now.AddMonths(-1);
            var seasonKey = $"{target.Year}-{target.Month:D2}";

            _logger.LogInformation("[RewardTop1] Start job for season {SeasonKey}", seasonKey);

            var top1Couple = await _unitOfWork.Leaderboards.GetFirstAsync(
                l => l.PeriodType == "monthly" &&
                     l.SeasonKey == seasonKey &&
                     l.Status == "ACTIVE" &&
                     l.RankPosition == 1
            );

            if (top1Couple == null)
            {
                _logger.LogWarning("[RewardTop1] No top1 found for season {SeasonKey}", seasonKey);
                return;
            }

            var couple = await _unitOfWork.CoupleProfiles.GetByIdAsync(top1Couple.CoupleId);
            if (couple == null || couple.Status == CoupleProfileStatus.INACTIVE.ToString() || couple.IsDeleted == true)
            {
                _logger.LogWarning("[RewardTop1] Invalid couple {CoupleId}", top1Couple.CoupleId);
                return;
            }

            if (top1Couple.PeriodStart == null || top1Couple.PeriodEnd == null)
            {
                _logger.LogError("[RewardTop1] Period null for leaderboard {Id}", top1Couple.Id);
                return;
            }

            var memberIds = new List<int> { couple.MemberId1, couple.MemberId2 };

            // Check if already rewarded
            var exists = await _unitOfWork.MemberAccessories.HasRewarded(memberIds, TOP_1_KING_ACCESSORY_ID, TOP_1_QUEEN_ACCESSORY_ID, top1Couple.PeriodStart.Value, top1Couple.PeriodEnd.Value);
            if (exists)
            {
                _logger.LogInformation("[RewardTop1] Already rewarded for season {SeasonKey}", seasonKey);
                return;
            }

            var members = await _unitOfWork.MembersProfile.GetByIdsAsync(memberIds);

            var newItems = new List<Data.Entities.MemberAccessory>();

            foreach (var m in members)
            {
                int accessoryId;
                if (m.Gender.ToUpper() == "MALE")
                    accessoryId = TOP_1_KING_ACCESSORY_ID;
                else if (m.Gender.ToUpper() == "FEMALE")
                    accessoryId = TOP_1_QUEEN_ACCESSORY_ID;
                else
                    accessoryId = TOP_1_KING_ACCESSORY_ID;

                newItems.Add(new Data.Entities.MemberAccessory
                {
                    MemberId = m.Id,
                    AccessoryId = accessoryId,
                    AcquiredAt = now,
                    ExpiredAt = top1Couple.PeriodEnd,
                    IsEquipped = false
                });
            }

            if (newItems.Count > 0)
            {
                var notifications = new List<Data.Entities.Notification>();

                var memberDict = members.ToDictionary(m => m.Id);

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    _logger.LogInformation("[RewardTop1] Creating {Count} accessories", newItems.Count);

                    await _unitOfWork.MemberAccessories.AddRangeAsync(newItems);
                    await _unitOfWork.SaveChangesAsync();

                    // Notify
                    foreach (var item in newItems)
                    {
                        if (!memberDict.TryGetValue(item.MemberId.Value, out var member))
                            continue;

                        notifications.Add(new Data.Entities.Notification
                        {
                            UserId = member.UserId,
                            Title = "🏆 Top 1 tháng!",
                            Message = $"Chúc mừng! Bạn và người ấy đã đạt Top 1 tháng {seasonKey}. Phần thưởng độc quyền đã được trao 🎉",
                            Type = NotificationType.SYSTEM.ToString(),
                            ReferenceType = ReferenceType.MEMBER_ACCESSORY.ToString(),
                            ReferenceId = item.Id,
                            CreatedAt = now,
                            IsRead = false,
                        });
                    }

                    _logger.LogInformation("[RewardTop1] Creating {Count} notifications", notifications.Count);

                    await _unitOfWork.Notifications.AddRangeAsync(notifications);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("[RewardTop1] Commit success for season {SeasonKey}", seasonKey);

                    foreach (var noti in notifications)
                    {
                        BackgroundJob.Enqueue<INotificationWorker>(x => x.SendPushNotificationAsync(noti.Id));
                    }

                    _logger.LogInformation("[RewardTop1] Enqueued {Count} push notifications", notifications.Count);

                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();

                    _logger.LogError(ex, "[RewardTop1] Failed for season {SeasonKey}", seasonKey);

                    throw;
                }
            }
        }
    }
}
