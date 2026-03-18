
using capstone_backend.Business.DTOs.MemberSubscription;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberSubscriptionService
    {
        Task<TransactionResponse> CheckPaymentStatusAsync(int userId, string orderId);
    }
}
