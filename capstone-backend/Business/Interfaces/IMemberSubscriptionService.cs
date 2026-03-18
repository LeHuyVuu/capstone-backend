
using capstone_backend.Business.DTOs.MemberSubscription;

namespace capstone_backend.Business.Interfaces
{
    public interface IMemberSubscriptionService
    {
        Task<MemberSubscriptionResponse> CheckPaymentStatusAsync(int userId, int transactionId);
    }
}
