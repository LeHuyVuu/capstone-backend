using capstone_backend.Business.DTOs.Voucher;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class GetMemberVoucherTransactionsRequestValidator : AbstractValidator<GetMemberVoucherTransactionsRequest>
    {
        public GetMemberVoucherTransactionsRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("ToDate phải lớn hơn hoặc bằng FromDate")
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

            RuleFor(x => x.SortBy)
                .Must(BeValidSortBy)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
                .WithMessage("SortBy chỉ chấp nhận: createdAt hoặc updatedAt.");

            RuleFor(x => x.OrderBy)
                .Must(BeValidOrderBy)
                .When(x => !string.IsNullOrWhiteSpace(x.OrderBy))
                .WithMessage("OrderBy chỉ chấp nhận: asc hoặc desc.");
        }

        private bool BeValidSortBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy)) return true;

            var normalized = sortBy.Trim().ToLower();
            return normalized == "createdat" || normalized == "updatedat";
        }

        private bool BeValidOrderBy(string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy)) return true;

            var normalized = orderBy.Trim().ToLower();
            return normalized == "asc" || normalized == "desc";
        }
    }
}
