using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface IFcmService
    {
        Task<string> SendNotificationAsync(string token, SendNotificationRequest request);
        Task<string> SendMultiNotificationAsync(List<string> tokens, SendNotificationRequest request);
    }
}
