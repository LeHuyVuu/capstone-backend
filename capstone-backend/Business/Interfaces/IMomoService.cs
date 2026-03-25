using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Wallet;

namespace capstone_backend.Business.Interfaces
{
    public interface IMomoService
    {
        Task<MomoLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
        Task<MomoLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request);
        Task<bool> VerifyPaymentProcessing(MomoIpnRequest request);
    }
}
