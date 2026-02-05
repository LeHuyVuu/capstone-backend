using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotificationService(NotificationRequest request);
        Task SendNotificationAsync(NotificationRequest request);
        Task SendPushNotificationAsync(string token);
        Task<PagedResult<NotificationResponse>> GetNotificationsByUserIdAsync(int userId, string type, int pageNumber = 1, int pageSize = 10);
        Task<int> MarkReadAsync(int notificationId, int userId);
        Task<int> MarkReadAllAsync(int userId);
    }
}
