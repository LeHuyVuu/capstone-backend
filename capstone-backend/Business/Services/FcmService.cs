using Azure.Core;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Repositories;
using FirebaseAdmin.Messaging;

namespace capstone_backend.Business.Services
{
    public class FcmService : IFcmService
    {
        private readonly FirebaseMessaging _messaging;
        private readonly IUnitOfWork _unitOfWork;

        public FcmService(IUnitOfWork unitOfWork)
        {
            _messaging = FirebaseMessaging.DefaultInstance;
            _unitOfWork = unitOfWork;
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
                    var tokensToRemove = new List<string>();

                    for (int i = 0; i < response.Responses.Count(); i++)
                    {
                        var result = response.Responses[i];
                        if (!result.IsSuccess)
                        {
                            var failedToken = tokens[i];
                            if (result.Exception is FirebaseMessagingException ex)
                            {
                                if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered || 
                                    ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                                    ex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
                                {
                                    tokensToRemove.Add(tokens[i]);
                                }
                            }
                        }
                    }

                    if (tokensToRemove.Any())
                    {
                        await _unitOfWork.DeviceTokens.RemoveRangeByTokensAsync(tokensToRemove);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return $"Success: {response.SuccessCount}, Fail: {response.FailureCount}";
            }
            catch (FirebaseMessagingException ex)
            {
                throw;
            }
        }

        public async Task<string> SendMultiDataOnlyNotificationAsync(List<string> tokens, SendNotificationRequest request)
        {
            if (tokens == null || !tokens.Any())
                return "No tokens provided";

            var data = new Dictionary<string, string>(request.Data ?? new Dictionary<string, string>());

            if (!string.IsNullOrWhiteSpace(request.Title))
                data["title"] = request.Title;

            if (!string.IsNullOrWhiteSpace(request.Body))
                data["body"] = request.Body;

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                data["imageUrl"] = request.ImageUrl;

            if (!data.Any())
                return "Empty data";

            try
            {
                var multicastMessage = new MulticastMessage
                {
                    Tokens = tokens,
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "5" }
                        },
                        Aps = new Aps
                        {
                            ContentAvailable = true
                        }
                    }
                };

                var response = await _messaging.SendEachForMulticastAsync(multicastMessage);

                if (response.FailureCount > 0)
                {
                    var tokensToRemove = new List<string>();

                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        var result = response.Responses[i];
                        if (!result.IsSuccess && result.Exception is FirebaseMessagingException ex)
                        {
                            if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                                ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                                ex.MessagingErrorCode == MessagingErrorCode.SenderIdMismatch)
                            {
                                tokensToRemove.Add(tokens[i]);
                            }
                        }
                    }

                    if (tokensToRemove.Any())
                    {
                        await _unitOfWork.DeviceTokens.RemoveRangeByTokensAsync(tokensToRemove);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                return $"Success: {response.SuccessCount}, Fail: {response.FailureCount}";
            }
            catch (FirebaseMessagingException)
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
                    ChannelId = "default_channel",
                    Priority = NotificationPriority.HIGH,
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
