using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class UpdateVoucherRequestValidator : AbstractValidator<UpdateVoucherRequest>
    {
        public UpdateVoucherRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống")
                .MaximumLength(100).WithMessage("Tiêu đề quá dài")
                .When(x => x.Title != null);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả không được để trống")
                .When(x => x.Description != null);

            RuleFor(x => x.VoucherPrice)
                .GreaterThan(0).WithMessage("Giá voucher phải lớn hơn 0");

            RuleFor(x => x.DiscountType)
                .Must(BeValidDiscountType)
                .When(x => !string.IsNullOrWhiteSpace(x.DiscountType))
                .WithMessage("DiscountType phải là FIXED_AMOUNT hoặc PERCENTAGE");

            RuleFor(x => x.DiscountType)
                .NotEmpty().WithMessage("DiscountType là bắt buộc khi truyền DiscountAmount hoặc DiscountPercent")
                .When(x => x.DiscountAmount.HasValue || x.DiscountPercent.HasValue);

            RuleFor(x => x)
                .Must(x => !(x.DiscountAmount.HasValue && x.DiscountPercent.HasValue))
                .WithMessage("Chỉ được truyền một trong hai: DiscountAmount hoặc DiscountPercent");

            RuleFor(x => x.DiscountAmount)
                .GreaterThan(0).WithMessage("Số tiền giảm phải lớn hơn 0")
                .When(x => string.Equals(x.DiscountType, VoucherDiscountType.FIXED_AMOUNT.ToString(), StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.DiscountPercent)
                .Null().WithMessage("Không được nhập Percent khi dùng Fixed Amount")
                .When(x => string.Equals(x.DiscountType, VoucherDiscountType.FIXED_AMOUNT.ToString(), StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(1, 100).WithMessage("Phần trăm giảm phải từ 1 đến 100")
                .When(x => string.Equals(x.DiscountType, VoucherDiscountType.PERCENTAGE.ToString(), StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.DiscountAmount)
                .Null().WithMessage("Không được nhập Amount khi dùng Percentage")
                .When(x => string.Equals(x.DiscountType, VoucherDiscountType.PERCENTAGE.ToString(), StringComparison.OrdinalIgnoreCase));

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải ít nhất là 1")
                .When(x => x.Quantity.HasValue);

            RuleFor(x => x.UsageLimitPerMember)
                .GreaterThan(0).WithMessage("Giới hạn sử dụng mỗi thành viên phải lớn hơn 0")
                .When(x => x.UsageLimitPerMember.HasValue);

            RuleFor(x => x.UsageValidDays)
                .InclusiveBetween(1, 365).WithMessage("UsageValidDays phải từ 1 đến 365 ngày")
                .When(x => x.UsageValidDays.HasValue);

            RuleFor(x => x.StartDate)
                .Must(x => !x.HasValue || x.Value >= DateTime.UtcNow.AddSeconds(-5))
                .WithMessage("Ngày bắt đầu không được ở quá khứ");

            RuleFor(x => x.EndDate)
                .Must(x => !x.HasValue || x.Value >= DateTime.UtcNow.AddSeconds(-5))
                .WithMessage("Ngày kết thúc không được ở quá khứ");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.VenueLocationIds)
                .Must(x => x == null || x.Count > 0)
                .WithMessage("Phải chọn ít nhất 1 địa điểm nếu có truyền VenueLocationIds.");

            RuleForEach(x => x.VenueLocationIds!)
                .GreaterThan(0).WithMessage("Mỗi VenueLocationId phải lớn hơn 0.")
                .When(x => x.VenueLocationIds != null);
        }

        private bool BeValidDiscountType(string? discountType)
        {
            if (string.IsNullOrWhiteSpace(discountType)) return true;

            return string.Equals(discountType, VoucherDiscountType.FIXED_AMOUNT.ToString(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(discountType, VoucherDiscountType.PERCENTAGE.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
