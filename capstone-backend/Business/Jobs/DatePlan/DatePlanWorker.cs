
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Notification;
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
        private readonly INotificationWorker _notificationWorker;


        public DatePlanWorker(IUnitOfWork unitOfWork, ILogger<DatePlanWorker> logger, IServiceProvider serviceProvider, INotificationService notificationService, INotificationWorker notificationWorker)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _notificationService = notificationService;
            _notificationWorker = notificationWorker;
        }

        [JobDisplayName("End DatePlan #{0}")]
        public async Task EndDatePlanAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.IN_PROGRESS.ToString())
            {
                _logger.LogInformation($"[SOFT END] Ending DatePlan #{datePlanId}");

                plan.Status = DatePlanStatus.COMPLETED.ToString();

                // todo: notify users
                var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

                await SendNotificationAsync(
                    new List<int> { userId1, userId2 },
                    NotificationTemplate.DatePlan.TitleDatePlanSoftEnded,
                    NotificationTemplate.DatePlan.GetDatePlanSoftEndedBody(plan.Title),
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

                var time = TimeOnly.FromDateTime(TimezoneUtil.ToVietNamTime(plan.PlannedStartAt.Value));


                string title = "";
                string body = "";

                if (type == "PRIMARY")
                {
                    title = NotificationTemplate.DatePlan.TitleReminderPrimary;
                    body = NotificationTemplate.DatePlan.GetReminderPrimaryBody(plan.Title, time);
                }
                else if (type == "SECONDARY")
                {
                    title = NotificationTemplate.DatePlan.TitleReminderSecondary;
                    body = NotificationTemplate.DatePlan.GetReminderSecondaryBody(plan.Title, time);
                }

                // Send realtime notification to couple members
                var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

                await SendNotificationAsync(
                    new List<int> { userId1, userId2 },
                    title,
                    body,
                    plan);

                await CleanupJobAsync(datePlanId, DatePlanJobType.REMINDER.ToString());
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
            var job = await _unitOfWork.DatePlanJobs.GetByDatePlanIdAndJobTypeAsync(datePlanId, jobType);

            if (job != null)
            {
                _unitOfWork.DatePlanJobs.Delete(job);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Clean up all jobs soon related to a date plan
        public async Task CleanupAllJobsAsync(int datePlanId)
        {
            var jobs = await _unitOfWork.DatePlanJobs.GetAllByDatePlanIdAsync(datePlanId);

            foreach (var job in jobs)
            {
                BackgroundJob.Delete(job.JobId);
            }

            if (jobs != null && jobs.Any())
            {
                _unitOfWork.DatePlanJobs.DeleteRange(jobs);
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
                Type = NotificationType.DATE_PLAN.ToString(),
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
                        { NotificationKeys.Type, NotificationType.DATE_PLAN.ToString() },
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

        [JobDisplayName("Auto Close Expired DatePlans")]
        public async Task AutoCloseExpiredDatePlanAsync()
        {
            var thresholdTime = DateTime.UtcNow.AddHours(-1);

            var expiredPlans = await _unitOfWork.DatePlans.GetAllExpiredPlansAsync(thresholdTime);

            if (!expiredPlans.Any())
            {
                _logger.LogInformation("[AUTO CLOSE] No expired DatePlans found.");
                return;
            }

            foreach (var plan in expiredPlans)
            {
                try
                {
                    if (plan.Status != DatePlanStatus.IN_PROGRESS.ToString())
                    {
                        _logger.LogInformation($"[AUTO CLOSE] Skipping DatePlan #{plan.Id} as its status is {plan.Status}");
                        continue;
                    }
                    plan.Status = DatePlanStatus.COMPLETED.ToString();
                    plan.CompletedAt = DateTime.UtcNow;

                    // Notify users
                    var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);
                    await SendNotificationAsync(
                        new List<int> { userId1, userId2 },
                        NotificationTemplate.DatePlan.TitleDatePlanAutoClosed,
                        NotificationTemplate.DatePlan.GetDatePlanAutoClosedBody(plan.Title),
                        plan);

                    await CleanupAllJobsAsync(plan.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[AUTO CLOSE] Error while auto closing DatePlan #{plan.Id}");
                }
            }

            _unitOfWork.DatePlans.UpdateRange(expiredPlans);
            await _unitOfWork.SaveChangesAsync();
        }

        [JobDisplayName("Notify DatePlan Rejected #{0}")]
        public async Task SendRejectedNotificationAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetFirstAsync(dp => dp.Id == datePlanId && dp.IsDeleted == false, dp => dp.Include(dp => dp.OrganizerMember));
            if (plan == null)
                return;

            var organizer = plan.OrganizerMember;

            var notification = new Data.Entities.Notification
            {
                UserId = organizer.UserId,
                Title = NotificationTemplate.DatePlan.TitleDatePlanRejected,
                Message = NotificationTemplate.DatePlan.GetDatePlanRejectedBody(plan.Title),
                Type = NotificationType.DATE_PLAN.ToString(),
                ReferenceId = plan.Id,
                ReferenceType = ReferenceType.DATE_PLAN.ToString()
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            await _notificationWorker.SendPushNotificationAsync(notification.Id);
        }

        [JobDisplayName("Notify DatePlan Cancelled #{0}")]
        public async Task SendCancelledNotificationAsync(int datePlanId)
        {

            var plan = await _unitOfWork.DatePlans.GetFirstAsync(dp => dp.Id == datePlanId && dp.IsDeleted == false);
            if (plan == null)
                return;

            var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

            var notifications = new List<Data.Entities.Notification>
            {
                new Data.Entities.Notification
                {
                    UserId = userId1,
                    Title = NotificationTemplate.DatePlan.TitleDatePlanCancelled,
                    Message = NotificationTemplate.DatePlan.GetDatePlanCancelledBody(plan.Title),
                    Type = NotificationType.DATE_PLAN.ToString(),
                    ReferenceId = plan.Id,
                    ReferenceType = ReferenceType.DATE_PLAN.ToString()
                },
                new Data.Entities.Notification
                {
                    UserId = userId2,
                    Title = NotificationTemplate.DatePlan.TitleDatePlanCancelled,
                    Message = NotificationTemplate.DatePlan.GetDatePlanCancelledBody(plan.Title),
                    Type = NotificationType.DATE_PLAN.ToString(),
                    ReferenceId = plan.Id,
                    ReferenceType = ReferenceType.DATE_PLAN.ToString()
                }
            };

            await _unitOfWork.Notifications.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            // Replace in SendCancelledNotificationAsync
            foreach (var notification in notifications)
            {
                await _notificationWorker.SendPushNotificationAsync(notification.Id);
            }
        }

        [JobDisplayName("Notify DatePlan Completed #{0}")]
        public async Task SendCompletedNotificationAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);
            if (plan == null) return;

            var (userId1, userId2) = await _unitOfWork.CoupleProfiles.GetCoupleUserIdsAsync(plan.CoupleId);

            var notifications = new List<Data.Entities.Notification>
            {
                new Data.Entities.Notification
                {
                    UserId = userId1,
                    Title = NotificationTemplate.DatePlan.TitleDatePlanCompleted,
                    Message = NotificationTemplate.DatePlan.GetDatePlanCompletedBody(plan.Title),
                    Type = NotificationType.DATE_PLAN.ToString(),
                    ReferenceId = plan.Id,
                    ReferenceType = ReferenceType.DATE_PLAN.ToString()
                },
                new Data.Entities.Notification
                {
                    UserId = userId2,
                    Title = NotificationTemplate.DatePlan.TitleDatePlanCompleted,
                    Message = NotificationTemplate.DatePlan.GetDatePlanCompletedBody(plan.Title),
                    Type = NotificationType.DATE_PLAN.ToString(),
                    ReferenceId = plan.Id,
                    ReferenceType = ReferenceType.DATE_PLAN.ToString()
                }
            };

            await _unitOfWork.Notifications.AddRangeAsync(notifications);
            await _unitOfWork.SaveChangesAsync();

            // Replace in SendCompletedNotificationAsync
            foreach (var notification in notifications)
            {
                await _notificationWorker.SendPushNotificationAsync(notification.Id);
            }
        }
    }
}
