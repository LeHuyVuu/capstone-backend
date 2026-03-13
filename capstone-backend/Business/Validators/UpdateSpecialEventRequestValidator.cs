using capstone_backend.Business.DTOs.SpecialEvent;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class UpdateSpecialEventRequestValidator : AbstractValidator<UpdateSpecialEventRequest>
{
    public UpdateSpecialEventRequestValidator()
    {
        RuleFor(x => x.EventName)
            .MaximumLength(200).WithMessage("Tên sự kiện không được vượt quá 200 ký tự")
            .When(x => !string.IsNullOrEmpty(x.EventName));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Validation khi cả StartDate và EndDate đều được cung cấp
        RuleFor(x => x)
            .Must(x => {
                if (!x.StartDate.HasValue || !x.EndDate.HasValue)
                    return true;
                
                // Nếu IsYearly = true, so sánh theo ngày/tháng
                if (x.IsYearly == true)
                {
                    return x.EndDate.Value.Month > x.StartDate.Value.Month || 
                           (x.EndDate.Value.Month == x.StartDate.Value.Month && 
                            x.EndDate.Value.Day > x.StartDate.Value.Day);
                }
                
                // Nếu IsYearly = false hoặc null, so sánh đầy đủ
                return x.EndDate.Value > x.StartDate.Value;
            })
            .WithMessage("Ngày kết thúc phải sau ngày bắt đầu")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
