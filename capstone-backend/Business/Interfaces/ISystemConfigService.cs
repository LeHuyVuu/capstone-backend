
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SystemConfig;

namespace capstone_backend.Business.Interfaces
{
    public interface ISystemConfigService
    {
        Task<string> GetValueAsync(string key);
        Task<int> GetIntValueAsync(string key);
        Task<decimal> GetDecimalValueAsync(string key);

        Task<PagedResult<SystemConfigResponse>> GetAllConfigsAsync(int pageNumber, int pageSize);
        Task<SystemConfigResponse> GetByKeyAsync(string key);
        Task<SystemConfigResponse> UpdateConfigAsync(UpdateSystemConfigRequest request);
    }
}
