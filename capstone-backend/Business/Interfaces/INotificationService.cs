using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateNotificationService(int userId, NotificationRequest request);
        Task SendNotificationAsync(int userId, NotificationRequest request);
        Task SendNotificationUsersAsync(List<int> userIds, NotificationRequest reuest);
        Task<PagedResult<NotificationResponse>> GetNotificationsByUserIdAsync(int userId, string type, int pageNumber = 1, int pageSize = 10);
        Task<int> MarkReadAsync(int notificationId, int userId);
        Task<int> MarkReadAllAsync(int userId);
    }
}
