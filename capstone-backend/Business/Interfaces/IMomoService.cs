using capstone_backend.Business.DTOs.Momo;

namespace capstone_backend.Business.Interfaces
{
    public interface IMomoService
    {
        Task<MomoLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
    }
}
