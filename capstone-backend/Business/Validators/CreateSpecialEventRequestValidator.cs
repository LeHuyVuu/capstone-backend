using capstone_backend.Business.DTOs.SpecialEvent;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class CreateSpecialEventRequestValidator : AbstractValidator<CreateSpecialEventRequest>
{
    public CreateSpecialEventRequestValidator()
    {
        RuleFor(x => x.EventName)
            .NotEmpty().WithMessage("Tên sự kiện không được để trống")
            .MaximumLength(200).WithMessage("Tên sự kiện không được vượt quá 200 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu không được để trống");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Ngày kết thúc không được để trống");

        // Validation cho sự kiện hằng năm (IsYearly = true)
        RuleFor(x => x)
            .Must(x => x.EndDate.Month > x.StartDate.Month || 
                      (x.EndDate.Month == x.StartDate.Month && x.EndDate.Day > x.StartDate.Day))
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu (so sánh theo ngày/tháng)")
            .When(x => x.IsYearly);

        // Validation cho sự kiện một lần (IsYearly = false)
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu")
            .When(x => !x.IsYearly);
    }
}
