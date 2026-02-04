using capstone_backend.Business.DTOs.Notification;
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

        public async Task<int> RegisterDeviceTokenAsync(int userId, RegisterDeviceTokenRequest request)
        {
			try
			{
                // Check if the device token already exists for the user
                var existingToken = await _unitOfWork.DeviceTokens.GetByTokenAsync(request.Token);
                if (existingToken != null && existingToken.UserId == userId)
                    throw new Exception("Device token already registered");

                var deviceToken = new DeviceToken()
                {
                    UserId = userId,
                    Token = request.Token,
                    Platform = request.Platform
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
