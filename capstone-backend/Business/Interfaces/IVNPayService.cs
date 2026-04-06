using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.VNPay;

namespace capstone_backend.Business.Interfaces
{
    public interface IVNPayService
    {
        Task<VNPayLinkResponse> ProcessMemberSubscriptionPaymentAsync(int userId, ProcessMemberSubscriptionPaymentRequest request);
        Task<VNPayReturnDto?> VerifyPaymentProcessingAsync(IQueryCollection requestData);
    }
}
