namespace capstone_backend.Business.Interfaces
{
    public interface IDeviceTokenService
    {
        Task<int> RegisterDeviceTokenAsync(int userId, string token, string? platform);
    }
}
