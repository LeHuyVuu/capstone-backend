
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using System.Numerics;

namespace capstone_backend.Business.Jobs.Review
{
    public class ReviewWorker : IReviewWorker
    {
        private readonly INotificationService _notificationService;
        private readonly IFcmService? _fcmService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewWorker> _logger;

        public ReviewWorker(INotificationService notificationService, IServiceProvider serviceProvider, IUnitOfWork unitOfWork, ILogger<ReviewWorker> logger)
        {
            _notificationService = notificationService;
            _fcmService = serviceProvider.GetService<IFcmService>();
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task SendReviewNotificationAsync(int checkInHistoryId)
        {
            var checkInHistory = await _unitOfWork.CheckInHistories.GetByIdAsync(checkInHistoryId);

            if (checkInHistory == null)
            {
                _logger.LogWarning("Check-in history with ID {CheckInHistoryId} not found.", checkInHistoryId);
                return;
            }

            var member = await _unitOfWork.MembersProfile.GetByIdAsync(checkInHistory.MemberId);
            if (member == null)
            {
                _logger.LogWarning("Member with ID {MemberId} not found.", checkInHistory.MemberId);
                return;
            }

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(checkInHistory.VenueId);
            if (venue == null)
            {
                _logger.LogWarning("Venue with ID {VenueId} not found.", checkInHistory.VenueId);
                return;
            }

            // Send notification logic goes here
            await _notificationService.SendNotificationAsync(
                member.UserId,
                new NotificationRequest
                {
                    Title = NotificationTemplate.Review.TitleReviewRequest,
                    Message = NotificationTemplate.Review.GetReviewRequestBody(venue.Name),
                    Type = NotificationType.LOCATION.ToString(),
                    ReferenceId = checkInHistoryId,
                    ReferenceType = ReferenceType.CHECK_IN_HISTORY.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        { NotificationKeys.RefId, venue.Id.ToString() },
                        { NotificationKeys.RefType, ReferenceType.VENUE_LOCATION.ToString() }
                    }
                }
            );

            // Send Push Notification
            if (_fcmService != null)
            {
                var token = await _unitOfWork.DeviceTokens.GetTokenByUserId(member.UserId);
                if (token == null || !token.Any())
                {
                    _logger.LogInformation("No device tokens found for user ID {UserId}.", member.UserId);
                    return;
                }

                await _fcmService.SendNotificationAsync(
                    token,
                    new SendNotificationRequest
                    {
                        Title = NotificationTemplate.Review.TitleReviewRequest,
                        Body = NotificationTemplate.Review.GetReviewRequestBody(venue.Name),
                        Data = new Dictionary<string, string>
                        {
                            { NotificationKeys.Type, NotificationType.LOCATION.ToString() },
                            { NotificationKeys.RefId, checkInHistoryId.ToString() },
                            { NotificationKeys.RefType, ReferenceType.CHECK_IN_HISTORY.ToString() },
                            { "venueLocationId", venue.Id.ToString() }
                        }
                    }
                );
            }
        }
    }
}
