using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using FirebaseAdmin.Messaging;

namespace capstone_backend.Business.Services
{
    public class FcmService : IFcmService
    {
        private readonly FirebaseMessaging _messaging;

        public FcmService()
        {
            _messaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task<string> SendMultiNotificationAsync(List<string> tokens, SendNotificationRequest request)
        {
            // Validate input
            if (tokens == null || !tokens.Any())
            {
                return "No tokens provided";
            }

            try
            {
                var multicastMessage = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = CreateNotification(request),
                    Data = request.Data,
                    Android = CreateAndroidConfig(request),
                    Apns = CreateApnsConfig(request),
                    Webpush = CreateWebpushConfig(request)
                };

                var response = await _messaging.SendEachForMulticastAsync(multicastMessage);

                if (response.FailureCount > 0)
                {
                    foreach (var failure in response.Responses.Where(r => !r.IsSuccess))
                    {
                        // todo: remove invalid tokens from database
                    }
                }

                return $"Success: {response.SuccessCount}, Fail: {response.FailureCount}";
            }
            catch (FirebaseMessagingException ex)
            {
                throw;
            }
        }

        public async Task<string> SendNotificationAsync(string token, SendNotificationRequest request)
        {
            if (string.IsNullOrEmpty(token)) return "Empty token";

            try
            {
                var message = new Message
                {
                    Token = token,
                    Notification = CreateNotification(request),
                    Data = request.Data,
                    Android = CreateAndroidConfig(request),
                    Apns = CreateApnsConfig(request),
                    Webpush = CreateWebpushConfig(request)
                };

                var response = await _messaging.SendAsync(message);
                return response;
            }
            catch (FirebaseMessagingException ex)
            {
                throw;
            }
        }

        // Helper methods to create platform-specific configurations
        private Notification CreateNotification(SendNotificationRequest request)
        {
            return new Notification
            {
                Title = request.Title,
                Body = request.Body,
                ImageUrl = request.ImageUrl
            };
        }

        private AndroidConfig CreateAndroidConfig(SendNotificationRequest request)
        {
            return new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ChannelId = "default", 
                    Icon = "ic_notification",
                    Color = "#FF6B6B",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            };
        }

        private ApnsConfig CreateApnsConfig(SendNotificationRequest request)
        {
            return new ApnsConfig
            {
                Aps = new Aps
                {
                    Alert = new ApsAlert
                    {
                        Title = request.Title,
                        Body = request.Body
                    },
                    Badge = 1,
                    Sound = "default",
                    ContentAvailable = true,
                    MutableContent = true
                },
                FcmOptions = new ApnsFcmOptions
                {
                    ImageUrl = request.ImageUrl
                }
            };
        }

        private WebpushConfig CreateWebpushConfig(SendNotificationRequest request)
        {
            return new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = request.Title,
                    Body = request.Body,
                    Icon = request.ImageUrl ?? "icon.png"
                }
            };
        }
    }
}
