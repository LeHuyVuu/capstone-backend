using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface INotificationService
    {
        Task<int> CreateNotificationService(CreateNotificationRequest request);
    }
}
