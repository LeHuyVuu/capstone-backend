
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using static capstone_backend.Business.Common.NotificationTemplate;

namespace capstone_backend.Business.Jobs.DatePlan
{    
    public class DatePlanWorker : IDatePlanWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFcmService? _fcmService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<DatePlanWorker> _logger;

        public DatePlanWorker(IUnitOfWork unitOfWork, ILogger<DatePlanWorker> logger, IServiceProvider serviceProvider, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _notificationService = notificationService;
        }

        [JobDisplayName("End DatePlan #{0}")]
        public async Task EndDatePlanAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.IN_PROGRESS.ToString())
            {
                _logger.LogInformation($"[END] Ending DatePlan #{datePlanId}");

                plan.Status = DatePlanStatus.COMPLETED.ToString();

                // todo: notify users
                var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

                await SendNotificationAsync(
                    new List<int> { userId1, userId2 },
                    NotificationTemplate.DatePlan.TitelDatePlanEnded,
                    NotificationTemplate.DatePlan.GetDatePlanEndedBody(plan.Title),
                    plan);

                await CleanupJobAsync(datePlanId, DatePlanJobType.END.ToString());

                await _unitOfWork.SaveChangesAsync();
            }
        }

        [JobDisplayName("Reminder for DatePlan #{0}")]
        public async Task SendReminderAsync(int datePlanId, string type)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.SCHEDULED.ToString())
            {
                _logger.LogInformation($"[REMINDER] Sending reminder for DatePlan #{datePlanId}");

                string title = "";
                string body = "";
                TimeOnly time = TimeOnly.FromDateTime(TimezoneUtil.ToVietNamTime(plan.PlannedStartAt.Value));

                if (type == "DAY")
                {
                    title = NotificationTemplate.DatePlan.TitleReminder1Day;
                    body = NotificationTemplate.DatePlan.GetReminder1DayBody(plan.Title, time);
                }
                else if (type == "HOUR")
                {
                    title = NotificationTemplate.DatePlan.TitleReminder1Hour;
                    body = NotificationTemplate.DatePlan.GetReminder1HourBody(plan.Title, time);
                }

                // Send realtime notification to couple members
                var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

                await SendNotificationAsync(
                    new List<int> { userId1, userId2 },
                    title,
                    body,
                    plan);

                await CleanupJobAsync(datePlanId, DatePlanJobType.END.ToString());
            }
        }

        [JobDisplayName("Start DatePlan #{0}")]
        public async Task StartDatePlanAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.SCHEDULED.ToString())
            {
                _logger.LogInformation($"[START] Starting DatePlan #{datePlanId}");

                plan.Status = DatePlanStatus.IN_PROGRESS.ToString();

                // todo: notify users
                var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);
                
                await SendNotificationAsync(
                    new List<int> { userId1, userId2 },
                    NotificationTemplate.DatePlan.TitleDatePlanStarted,
                    NotificationTemplate.DatePlan.GetDatePlanStartedBody(plan.Title),
                    plan);

                await CleanupJobAsync(datePlanId, DatePlanJobType.START.ToString());

                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Clean up done date plans
        private async Task CleanupJobAsync(int datePlanId, string jobType)
        {
            var job = await _unitOfWork.DatePlanJobs.GetByDatePlanIdAndJobType(datePlanId, jobType);

            if (job != null)
            {
                _unitOfWork.DatePlanJobs.Delete(job);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Send notification to users
        private async Task SendNotificationAsync(
            List<int> userIds,
            string title,
            string body,
            Data.Entities.DatePlan plan)
        {
            var request = new NotificationRequest
            {
                Title = title,
                Message = body,
                Type = NotificationType.SYSTEM.ToString(),
                ReferenceId = plan.Id,
                ReferenceType = ReferenceType.DATE_PLAN.ToString()
            };

            await _notificationService.SendNotificationUsersAsync(userIds, request);

            // Send push notification via FCM
            if (_fcmService != null)
            {
                var tokens = await _unitOfWork.DeviceTokens.GetByCoupleId(plan.CoupleId);
                var data = new Dictionary<string, string>
                    {
                        { NotificationKeys.Type, NotificationType.SYSTEM.ToString() },
                        { NotificationKeys.RefId, plan.Id.ToString() },
                        { NotificationKeys.RefType, ReferenceType.DATE_PLAN.ToString() }
                    };
                var pushRequest = new SendNotificationRequest
                {
                    Title = title,
                    Body = body,
                    Data = data
                };
                await _fcmService.SendMultiNotificationAsync(tokens, pushRequest);
            }
        }
    }
}
