using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class GetVoucherActivityRequestValidator : AbstractValidator<GetVoucherActivityRequest>
    {
        public GetVoucherActivityRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("ToDate phải lớn hơn hoặc bằng FromDate")
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

            RuleFor(x => x.OrderBy)
                .Must(BeValidOrderBy)
                .When(x => !string.IsNullOrWhiteSpace(x.OrderBy))
                .WithMessage("OrderBy chỉ chấp nhận: asc hoặc desc.");
        }

        private bool BeValidOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy)) return true;

            var normalized = orderBy.Trim().ToLower();
            return normalized == "asc" || normalized == "desc";
        }
    }
}
