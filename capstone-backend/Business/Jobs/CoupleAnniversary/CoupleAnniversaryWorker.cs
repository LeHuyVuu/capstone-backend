using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.CoupleAnniversary
{
    public class CoupleAnniversaryWorker : ICoupleAnniversaryWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IFcmService? _fcmService;
        private readonly ILogger<CoupleAnniversaryWorker> _logger;

        public CoupleAnniversaryWorker(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IServiceProvider serviceProvider,
            ILogger<CoupleAnniversaryWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _logger = logger;
        }

        [JobDisplayName("Send Anniversary Notifications")]
        public async Task SendAnniversaryNotificationsAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var currentMonth = today.Month;
                var currentDay = today.Day;

                _logger.LogInformation("[ANNIVERSARY] Checking anniversaries for {Month}/{Day}", currentMonth, currentDay);

                // Lấy tất cả couple có AniversaryDate trùng ngày/tháng (không cần năm)
                var couplesWithAnniversary = await _unitOfWork.Context.Set<Data.Entities.CoupleProfile>()
                    .Where(c => c.AniversaryDate.HasValue &&
                                c.AniversaryDate.Value.Month == currentMonth &&
                                c.AniversaryDate.Value.Day == currentDay &&
                                c.Status == CoupleProfileStatus.ACTIVE.ToString() &&
                                (c.IsDeleted == null || c.IsDeleted == false))
                    .Include(c => c.MemberId1Navigation)
                        .ThenInclude(m => m.User)
                    .Include(c => c.MemberId2Navigation)
                        .ThenInclude(m => m.User)
                    .ToListAsync();

                if (!couplesWithAnniversary.Any())
                {
                    _logger.LogInformation("[ANNIVERSARY] No couples with anniversary today");
                    return;
                }

                _logger.LogInformation("[ANNIVERSARY] Found {Count} couple(s) with anniversary today", couplesWithAnniversary.Count);

                foreach (var couple in couplesWithAnniversary)
                {
                    try
                    {
                        // Tính số năm kỷ niệm
                        var anniversaryDate = couple.AniversaryDate!.Value;
                        var yearsCount = today.Year - anniversaryDate.Year;

                        // Lấy userId của cả 2 người
                        var userId1 = couple.MemberId1Navigation?.UserId;
                        var userId2 = couple.MemberId2Navigation?.UserId;

                        if (!userId1.HasValue || !userId2.HasValue)
                        {
                            _logger.LogWarning("[ANNIVERSARY] Couple {CoupleId} missing user IDs", couple.id);
                            continue;
                        }

                        var coupleName = !string.IsNullOrEmpty(couple.CoupleName) 
                            ? couple.CoupleName 
                            : "Cặp đôi của bạn";

                        // Tạo notification cho cả 2 người
                        var title = "🎉 Chúc mừng kỷ niệm!";
                        var body = yearsCount > 0
                            ? $"Hôm nay là ngày kỷ niệm {yearsCount} năm của {coupleName}! Hãy tạo một kế hoạch hẹn hò đặc biệt để kỷ niệm ngày quan trọng này nhé! 💕"
                            : $"Hôm nay là ngày kỷ niệm của {coupleName}! Hãy tạo một kế hoạch hẹn hò đặc biệt để kỷ niệm ngày quan trọng này nhé! 💕";

                        var notificationRequest = new NotificationRequest
                        {
                            Title = title,
                            Message = body,
                            Type = NotificationType.SYSTEM.ToString(),
                            ReferenceType = "COUPLE_PROFILE",
                            ReferenceId = couple.id,
                            Data = new Dictionary<string, string>
                            {
                                { "coupleId", couple.id.ToString() },
                                { "anniversaryDate", anniversaryDate.ToString("dd/MM/yyyy") },
                                { "yearsCount", yearsCount.ToString() }
                            }
                        };

                        // Gửi notification cho cả 2 người
                        await _notificationService.SendNotificationUsersAsync(
                            new List<int> { userId1.Value, userId2.Value },
                            notificationRequest);

                        // Gửi push notification via FCM
                        if (_fcmService != null)
                        {
                            var tokens = await _unitOfWork.DeviceTokens.GetByCoupleId(couple.id);
                            if (tokens != null && tokens.Any())
                            {
                                var pushRequest = new SendNotificationRequest
                                {
                                    Title = title,
                                    Body = body,
                                    Data = new Dictionary<string, string>
                                    {
                                        { NotificationKeys.Type, NotificationType.SYSTEM.ToString() },
                                        { NotificationKeys.RefId, couple.id.ToString() },
                                        { NotificationKeys.RefType, "COUPLE_PROFILE" }
                                    }
                                };
                                await _fcmService.SendMultiNotificationAsync(tokens, pushRequest);
                            }
                        }

                        _logger.LogInformation(
                            "[ANNIVERSARY] Sent notifications for couple {CoupleId} ({CoupleName}) - {Years} years",
                            couple.id, coupleName, yearsCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ANNIVERSARY] Error sending notification for couple {CoupleId}", couple.id);
                    }
                }

                _logger.LogInformation("[ANNIVERSARY] Completed sending anniversary notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANNIVERSARY] Error in SendAnniversaryNotificationsAsync");
                throw;
            }
        }
    }
}
