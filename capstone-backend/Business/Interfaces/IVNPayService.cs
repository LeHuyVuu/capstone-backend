using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.VNPay;
using capstone_backend.Business.DTOs.Wallet;

namespace capstone_backend.Business.Interfaces
{
    public interface IVNPayService
    {
        Task<VNPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
        Task<VNPayLinkResponse> ProcessMemberWalletTopupAsync(int userId, CreateWalletTopupRequest request);
        Task<VNPayReturnDto?> VerifyPaymentProcessingAsync(IQueryCollection requestData);
        Task<TransactionResponse> CheckVNPAYTransactionStatusAsync(int userId, string orderId);
    }
}
