using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class DisableVoucherRequestValidator : AbstractValidator<DisableVoucherRequest>
    {
        public DisableVoucherRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do vô hiệu hóa voucher")
                .MaximumLength(500).WithMessage("Lý do vô hiệu hóa không được vượt quá 500 ký tự");
        }
    }
}
