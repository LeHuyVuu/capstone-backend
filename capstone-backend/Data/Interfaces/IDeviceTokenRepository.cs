using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IDeviceTokenRepository : IGenericRepository<DeviceToken>
    {
        Task<DeviceToken?> GetByTokenAsync(string token);
        Task<List<string>> GetByCoupleId(int coupleId);
        Task<string> GetTokenByUserId(int userId);
    }
}
