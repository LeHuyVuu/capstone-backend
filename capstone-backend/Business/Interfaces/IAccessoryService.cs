using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces
{
    public interface IAccessoryService
    {
        Task<PagedResult<AccessoryResponse>> GetShopAsync(int userId, GetAccessoryShopRequest query);
    }
}
