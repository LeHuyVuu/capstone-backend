using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace capstone_backend.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IFcmService? _fcmService;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<NotificationHub> hubContext, IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hubContext = hubContext;
            _fcmService = serviceProvider.GetService<IFcmService>();
        }

        public async Task<NotificationResponse> CreateNotificationService(NotificationRequest request)
        {
            try
            {
                var notification = _mapper.Map<Notification>(request);

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<NotificationResponse>(notification);

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<PagedResult<NotificationResponse>> GetNotificationsByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var (notification, totalCount) = await _unitOfWork.Notifications.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        n => n.UserId == userId,
                        n => n.OrderByDescending(x => x.CreatedAt)
                    );

                var response = _mapper.Map<List<NotificationResponse>>(notification);

                return new PagedResult<NotificationResponse>
                {
                    Items = response,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    PageNumber = pageNumber
                };

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task SendNotificationAsync(NotificationRequest request)
        {
            try
            {
                // 1. Save notification to database
                var notificationRes = await CreateNotificationService(request);
                if (notificationRes == null)
                {
                    throw new Exception("Failed to save notification.");
                }

                // 2. Send notification
                await _hubContext.Clients.Group($"User_{request.UserId}").SendAsync("ReceiveNotification", notificationRes);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task SendNotificationAsyncV2(string token)
        {
            try
            {
                var request = new SendNotificationRequest
                {
                    Token = token,
                    Title = "Test Notification",
                    Body = "This is a test notification message."
                };
               
                if (_fcmService == null)
                {
                    Console.WriteLine("[WARNING] FCM Service not configured. Skip push notification.");
                    return;
                }

                await _fcmService.SendNotificationAsync(request);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
