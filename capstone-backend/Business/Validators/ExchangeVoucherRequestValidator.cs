using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class ExchangeVoucherRequestValidator : AbstractValidator<ExchangeVoucherRequest>
    {
        public ExchangeVoucherRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotNull().WithMessage("Danh sách voucher đổi không được để trống")
                .Must(x => x != null && x.Count > 0).WithMessage("Danh sách voucher đổi không được để trống");

            RuleForEach(x => x.Items)
                .SetValidator(new ExchangeVoucherItemRequestValidator());

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));
        }
    }

    public class ExchangeVoucherItemRequestValidator : AbstractValidator<ExchangeVoucherItemRequest>
    {
        public ExchangeVoucherItemRequestValidator()
        {
            RuleFor(x => x.VoucherId)
                .GreaterThan(0).WithMessage("VoucherId phải lớn hơn 0");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0");
        }
    }
}
