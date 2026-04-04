using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.DTOs.Zalo;

namespace capstone_backend.Business.Interfaces
{
    public interface IZaloPayService
    {
        Task<TransactionResponse?> CheckWalletTopupStatusAsync(int userId, string appTransId);
        Task<ZaloPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
        Task<ZaloPayLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request);
        Task<bool> VerifyPaymentProcessing(ZaloPayCallbackRequest request);
    }
}
