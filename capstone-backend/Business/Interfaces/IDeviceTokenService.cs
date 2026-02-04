using capstone_backend.Business.DTOs.Notification;

namespace capstone_backend.Business.Interfaces
{
    public interface IDeviceTokenService
    {
        Task<int> RegisterDeviceTokenAsync(int userId, RegisterDeviceTokenRequest request);
    }
}
