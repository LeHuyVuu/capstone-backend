using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Contracts.SignalR;
using capstone_backend.Data.Entities;
using capstone_backend.Hubs;
using Hangfire.Logging.LogProviders;
using Microsoft.AspNetCore.SignalR;

namespace capstone_backend.Business.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        public async Task<NotificationResponse> CreateNotificationService(int userId, NotificationRequest request)
        {
            try
            {
                var notification = _mapper.Map<Notification>(request);
                notification.UserId = userId;

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                var response = _mapper.Map<NotificationResponse>(notification);
                response.Data = request.Data;

                return response;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<PagedResult<NotificationResponse>> GetNotificationsByUserIdAsync(int userId, string type, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var (notification, totalCount) = await _unitOfWork.Notifications.GetPagedAsync(
                        pageNumber,
                        pageSize,
                        n => n.UserId == userId && n.Type == type,
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

        public async Task<int> MarkReadAllAsync(int userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsByUserIdAsync(userId);
                if (notifications == null || !notifications.Any())
                    return 0;

                notifications.Select(n =>
                {
                    n.IsRead = true;

                    return n;
                }).ToList();

                _unitOfWork.Notifications.UpdateRange(notifications);
                var isSuccess = await _unitOfWork.SaveChangesAsync();

                // Send real-time update to client
                if (isSuccess > 0)
                {
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync(NotificationEvents.NotificationReadAll);
                    return isSuccess;
                }

                return 0;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> MarkReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAndUserIdAsync(notificationId, userId);
                if (notification == null)
                    throw new Exception("Notification not found.");

                if (notification.IsRead == true)
                    return 0;

                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);

                var isSuccess = await _unitOfWork.SaveChangesAsync();

                // Send real-time update to client
                if (isSuccess > 0)
                {
                    await _hubContext.Clients.Group($"User_{userId}").SendAsync(NotificationEvents.NotificationRead, notificationId);
                    return isSuccess;
                }

                return 0;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task SendNotificationAsync(int userId, NotificationRequest request)
        {
            try
            {
                // 1. Save notification to database
                var notificationRes = await CreateNotificationService(userId, request);
                if (notificationRes == null)
                {
                    throw new Exception("Failed to save notification.");
                }

                // 2. Send notification
                var (total, unread) = await _unitOfWork.Notifications.GetNotificationStatsByUserIdAsync(userId);
                var noti = new NotificationReceived()
                {
                    Notification = notificationRes,
                    Stats = new NotificationStats()
                    {
                        Total = total,
                        Unread = unread
                    }
                };

                // 3. Send real-time update to client
                await _hubContext.Clients.Group($"User_{userId}").SendAsync(NotificationEvents.NotificationReceived, noti);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task SendNotificationUsersAsync(List<int> userIds, NotificationRequest reuest)
        {
            try
            {
                var notifications = new List<Notification>();

                foreach (var userId in userIds)
                {
                    var notification = _mapper.Map<Notification>(reuest);
                    notification.UserId = userId;
                    notifications.Add(notification);
                }

                await _unitOfWork.Notifications.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();

                for (int i = 0; i < userIds.Count(); i++)
                {
                    var notiEntity = notifications[i];

                    var (total, unread) = await _unitOfWork.Notifications.GetNotificationStatsByUserIdAsync(userIds[i]);

                    var notiPayload = new NotificationReceived()
                    {
                        Notification = _mapper.Map<NotificationResponse>(notiEntity),
                        Stats = new NotificationStats()
                        {
                            Total = total,
                            Unread = unread
                        }
                    };

                    await _hubContext.Clients.Group($"User_{userIds[i]}").SendAsync(NotificationEvents.NotificationReceived, notiPayload);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
