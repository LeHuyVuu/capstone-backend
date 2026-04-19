
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberSubscriptionService
    {
        Task<bool> CancelSubscriptionAsync(int userId);
        Task<TransactionResponse> CheckPaymentStatusAsync(int userId, string orderId);
        Task<CurrentSubscriptionInfoResponse> GetCurrentSubscriptionInfoAsync(int userId);
        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task<PagedResult<SubscriptionPackageDto>> GetAvailablePackagesAsync(int pageNumber, int pageSize);
        Task<MemberSubscriptionResponse?> GetCurrentSubscriptionAsync(int userId);
        Task<PagedResult<TransactionResponse>> GetTransactionHistoryAsync(int userId, int pageNumber, int pageSize);
        Task<MemberSubscriptionPackage?> EnsureDefaultSubscriptionAsync(int userId);
    }
}
