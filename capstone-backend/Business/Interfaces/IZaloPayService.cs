using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Zalo;

namespace capstone_backend.Business.Interfaces
{
    public interface IZaloPayService
    {
        Task<ZaloPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
        Task<bool> VerifyPaymentProcessing(ZaloPayCallbackRequest request);
    }
}
