using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class GetVoucherItemsRequestValidator : AbstractValidator<GetVoucherItemsRequest>
    {
        public GetVoucherItemsRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0");

            RuleFor(x => x.Status)
                .Must(BeAValidStatus)
                .When(x => x.Status.HasValue)
                .WithMessage("Trạng thái chỉ chấp nhận: AVAILABLE, ACQUIRED, USED, EXPIRED, ENDED.");

            RuleFor(x => x.SortBy)
                .Must(BeValidSortBy)
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
                .WithMessage("SortBy chỉ chấp nhận: createdAt hoặc updatedAt.");

            RuleFor(x => x.OrderBy)
                .Must(BeValidOrderBy)
                .When(x => !string.IsNullOrWhiteSpace(x.OrderBy))
                .WithMessage("OrderBy chỉ chấp nhận: asc hoặc desc.");
        }

        private bool BeAValidStatus(VoucherItemStatus? status)
        {
            return status == VoucherItemStatus.AVAILABLE ||
                   status == VoucherItemStatus.ACQUIRED ||
                   status == VoucherItemStatus.USED ||
                   status == VoucherItemStatus.EXPIRED ||
                   status == VoucherItemStatus.ENDED;
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
