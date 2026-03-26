
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using System;

namespace capstone_backend.Business.Jobs.Notification
{
    public class NotificationWorker : INotificationWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFcmService? _fcmService;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger<NotificationWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _logger = logger;
        }

        public async Task SendPushNotificationAsync(int notificationId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null)
            {
                _logger.LogError("Notification with ID {NotificationId} not found", notificationId);
                return;
            }

            if (_fcmService != null)
            {
                var deviceTokens = await _unitOfWork.DeviceTokens.GetTokensByUserId(notification.UserId);
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    _logger.LogWarning("No device tokens found for user ID {UserId}", notification.UserId);
                    return;
                }

                // Send push
                var request = new SendNotificationRequest
                {
                    Title = notification.Title,
                    Body = notification.Message,
                    Data = new Dictionary<string, string>
                {
                    { NotificationKeys.Type, notification.Type },
                    { NotificationKeys.RefId, notification.ReferenceId.ToString() },
                    { NotificationKeys.RefType, notification.ReferenceType }
                }
                };

                await _fcmService.SendMultiNotificationAsync(deviceTokens, request);
            }
        }
    }
}
