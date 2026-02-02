using AutoMapper;
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

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _hubContext = hubContext;
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
    }
}
