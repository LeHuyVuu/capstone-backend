using AutoMapper;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;

namespace capstone_backend.Business.Services
{
    public class MemberSubscriptionService : IMemberSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MemberSubscriptionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TransactionResponse> CheckPaymentStatusAsync(int userId, string orderId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var orderParts = orderId.Split("_");
            if (orderParts.Length < 3)
                throw new Exception("Order ID không hợp lệ");
            var transactionId = IdEncoder.Decode(orderParts[2]);

            var tx = await _unitOfWork.Transactions.GetByIdAsync((int)transactionId);
            if (tx == null || tx.UserId != userId)
                throw new Exception("Giao dịch không tồn tại hoặc không thuộc về người dùng");

            if (tx.TransType != 3)
                throw new Exception("Giao dịch không phải thanh toán gói member");

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
            if (sub == null)
                throw new Exception("Không ghi nhận được gói đăng ký của member");

            var metadata = JsonConverterUtil.DeserializeOrDefault<MomoTransactionMetadata>(tx.ExternalRefCode);
            var response = _mapper.Map<TransactionResponse>(tx);
            response.PayUrl = metadata?.PayUrl;
            response.QrCodeUrl = metadata?.QrCodeUrl;
            response.DeepLink = metadata?.DeepLink;
            response.DeeplinkMiniApp = metadata?.DeeplinkMiniApp;

            response.MemberSubscriptionId = tx.DocNo;
            response.StartDate = sub.StartDate;
            response.EndDate = sub.EndDate;
            response.IsActive = sub.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString();

            return response;
        }

        public async Task<MemberSubscriptionResponse?> GetCurrentSubscriptionAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (sub == null)
                return null;

            var response = _mapper.Map<MemberSubscriptionResponse>(sub);
            return response;
        }
    }
}
