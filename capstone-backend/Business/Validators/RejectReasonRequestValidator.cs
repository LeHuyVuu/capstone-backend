using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class RejectReasonRequestValidator : AbstractValidator<RejectReasonRequest>
    {
        public RejectReasonRequestValidator()
        {
            RuleFor(x => x.RejectReason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do từ chối")
                .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự");
        }
    }
}
