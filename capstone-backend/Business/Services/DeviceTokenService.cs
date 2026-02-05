using Azure.Core;
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

        public async Task<int> DeleteDeviceTokenAsync(int userId, string deviceToken)
        {
            try
            {
                // Check if the device token already exists
                var existingToken = await _unitOfWork.DeviceTokens.GetByTokenAsync(deviceToken);

                if (existingToken == null)
                    return 0;

                if (existingToken.UserId != userId)
                    throw new UnauthorizedAccessException("Device token does not belong to current user");

                _unitOfWork.DeviceTokens.Delete(existingToken);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<int> RegisterDeviceTokenAsync(int userId, RegisterDeviceTokenRequest request)
        {
			try
			{
                // Check if the device token already exists
                var existingToken = await _unitOfWork.DeviceTokens.GetByTokenAsync(request.Token);

                if (existingToken == null)
                {
                    await _unitOfWork.DeviceTokens.AddAsync(new DeviceToken
                    {
                        UserId = userId,
                        Token = request.Token,
                        Platform = request.Platform,
                    });
                }
                else
                {
                    existingToken.UserId = userId;
                    existingToken.Platform = request.Platform;

                    _unitOfWork.DeviceTokens.Update(existingToken);
                }

                return await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception)
			{

				throw;
			}
        }
    }
}
