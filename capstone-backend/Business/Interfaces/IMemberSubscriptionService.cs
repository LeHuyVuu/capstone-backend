
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.SubscriptionPackage;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberSubscriptionService
    {
        Task<TransactionResponse> CheckPaymentStatusAsync(int userId, string orderId);
        Task<PagedResult<SubscriptionPackageDto>> GetAvailablePackagesAsync(int pageNumber, int pageSize);
        Task<MemberSubscriptionResponse?> GetCurrentSubscriptionAsync(int userId);
    }
}
