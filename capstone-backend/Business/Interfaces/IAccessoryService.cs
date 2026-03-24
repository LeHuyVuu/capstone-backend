using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces
{
    public interface IAccessoryService
    {
        Task<AccessoryDetailResponse?> GetDetailAsync(int userId, int accessoryId);
        Task<PagedResult<AccessoryResponse>> GetShopAsync(int userId, GetAccessoryShopRequest query);
        Task<PurchaseResponse> PurchaseAccessoryAsync(int userId, int accessoryId);
        Task<PagedResult<MyAccessoryResponse>> GetMyAccessoryAsync(int userId, GetMyAccessoryRequest query);
    }
}
