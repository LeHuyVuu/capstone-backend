using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class ValidateAndRedeemVoucherItemRequestValidator : AbstractValidator<ValidateAndRedeemVoucherItemRequest>
    {
        public ValidateAndRedeemVoucherItemRequestValidator()
        {
            RuleFor(x => x.ItemCode)
                .NotEmpty().WithMessage("Mã voucher không được để trống")
                .MaximumLength(100).WithMessage("Mã voucher không được vượt quá 100 ký tự");

            RuleFor(x => x.VenueLocationId)
                .GreaterThan(0).WithMessage("VenueLocationId phải lớn hơn 0");
        }
    }
}
