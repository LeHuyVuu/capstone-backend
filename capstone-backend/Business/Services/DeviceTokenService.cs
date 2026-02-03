using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services
{
    public class DeviceTokenService : IDeviceTokenService
    {
		private readonly IUnitOfWork _unitOfWork;

        public DeviceTokenService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> RegisterDeviceTokenAsync(int userId, string token, string? platform)
        {
			try
			{
                var deviceToken = new DeviceToken()
                {
                    UserId = userId,
                    Token = token
                };

                await _unitOfWork.DeviceTokens.AddAsync(deviceToken);
                return await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception)
			{

				throw;
			}
        }
    }
}
