
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SystemConfig;

namespace capstone_backend.Business.Interfaces
{
    public interface ISystemConfigService
    {
        Task<PagedResult<SystemConfigResponse>> GetAllConfigsAsync(int pageNumber, int pageSize);
    }
}
