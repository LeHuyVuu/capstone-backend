using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class GetMyVouchersRequestValidator : AbstractValidator<GetMyVouchersRequest>
    {
        public GetMyVouchersRequestValidator()
        {
            RuleFor(x => x.Status)
            .Must(BeAValidStatus)
            .When(x => x.Status.HasValue)
            .WithMessage("Trạng thái chỉ chấp nhận: ACQUIRED, USED, EXPIRED.");
        }

        private bool BeAValidStatus(VoucherItemStatus? status)
        {
            return status == VoucherItemStatus.ACQUIRED ||
                   status == VoucherItemStatus.USED ||
                   status == VoucherItemStatus.EXPIRED;
        }
    }
}
