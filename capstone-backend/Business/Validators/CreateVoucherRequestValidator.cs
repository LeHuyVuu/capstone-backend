using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Enums;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class CreateVoucherRequestValidator : AbstractValidator<CreateVoucherRequest>
    {
        public CreateVoucherRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống")
                .MaximumLength(100).WithMessage("Tiêu đề quá dài");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả là bắt buộc");

            // Check DiscountType
            RuleFor(x => x.DiscountType)
                .Must(x => x == VoucherDiscountType.FIXED_AMOUNT.ToString() || x == VoucherDiscountType.PERCENTAGE.ToString())
                .WithMessage("DiscountType phải là FIXED_AMOUNT hoặc PERCENTAGE");

            RuleFor(x => x)
                .Must(x => !(x.DiscountAmount.HasValue && x.DiscountPercent.HasValue))
                .WithMessage("Chỉ được truyền một trong hai: DiscountAmount hoặc DiscountPercent");

            // If FIXED_AMOUNT: amount should be > 0, percent should be null
            RuleFor(x => x.DiscountAmount)
                .NotEmpty().GreaterThan(0).WithMessage("Số tiền giảm phải lớn hơn 0")
                .When(x => x.DiscountType == VoucherDiscountType.FIXED_AMOUNT.ToString());

            RuleFor(x => x.DiscountPercent)
                .Null().WithMessage("Không được nhập Percent khi dùng Fixed Amount")
                .When(x => x.DiscountType == VoucherDiscountType.FIXED_AMOUNT.ToString());

            // If PERCENTAGE: percent should be between 1 and 100, amount should be null
            RuleFor(x => x.DiscountPercent)
                .NotEmpty().InclusiveBetween(1, 100).WithMessage("Phần trăm giảm phải từ 1 đến 100")
                .When(x => x.DiscountType == VoucherDiscountType.PERCENTAGE.ToString());

            RuleFor(x => x.DiscountAmount)
                .Null().WithMessage("Không được nhập Amount khi dùng Percentage")
                .When(x => x.DiscountType == VoucherDiscountType.PERCENTAGE.ToString());

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải ít nhất là 1");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.VenueLocationIds)
                .NotNull().WithMessage("VenueLocationIds không được null.")
                .Must(x => x.Count > 0).WithMessage("Phải chọn ít nhất 1 địa điểm.");

            RuleForEach(x => x.VenueLocationIds)
                .GreaterThan(0).WithMessage("Mỗi VenueLocationId phải lớn hơn 0.");

            RuleFor(x => x.UsageValidDays)
                .NotNull().WithMessage("UsageValidDays không được null.")
                .InclusiveBetween(1, 365).WithMessage("UsageValidDays phải từ 1 đến 365 ngày");
        }
    }
}
