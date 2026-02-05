using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface IFcmService
    {
        Task<string> SendNotificationAsync(SendNotificationRequest request);
    }
}
