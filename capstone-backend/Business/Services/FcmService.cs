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

        public async Task<string> SendNotificationAsync(SendNotificationRequest request)
        {
            try
            {
                var message = new Message
                {
                    Token = request.Token,
                    Notification = new Notification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        ImageUrl = request.ImageUrl
                    },
                    Data = request.data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Title = request.Title, 
                            Body = request.Body,   
                            ImageUrl = request.ImageUrl,
                            Sound = "default",
                            ChannelId = "default",
                            Icon = "ic_notification",
                            Color = "#FF6B6B",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    },
                    Apns = new ApnsConfig
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
                            ContentAvailable = true 
                        },
                        FcmOptions = new ApnsFcmOptions
                        {
                            ImageUrl = request.ImageUrl
                        }
                    },
                    Webpush = new WebpushConfig
                    {
                        Notification = new WebpushNotification
                        {
                            Title = request.Title,
                            Body = request.Body,
                            Icon = request.ImageUrl ?? "/icon.png"
                        }
                    }
                };

                var response = await _messaging.SendAsync(message);
                Console.WriteLine($"[INFO] FCM sent successfully");
                return response;
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"[ERROR] FCM failed: {ex.MessagingErrorCode} - {ex.Message}");
                throw new Exception($"FCM Error [{ex.MessagingErrorCode}]: {ex.Message}", ex);
            }
        }
    }
}
