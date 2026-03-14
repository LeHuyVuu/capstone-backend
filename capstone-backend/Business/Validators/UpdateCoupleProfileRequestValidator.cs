using capstone_backend.Business.DTOs.CoupleProfile;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class UpdateCoupleProfileRequestValidator : AbstractValidator<UpdateCoupleProfileRequest>
{
    public UpdateCoupleProfileRequestValidator()
    {
        // CoupleName validation
        RuleFor(x => x.CoupleName)
            .MaximumLength(100).WithMessage("Tên cặp đôi không được vượt quá 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.CoupleName));

        // StartDate validation
        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày bắt đầu không được là ngày trong tương lai")
            .When(x => x.StartDate.HasValue);

        // AniversaryDate validation
        RuleFor(x => x.AniversaryDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Ngày kỷ niệm phải sau hoặc bằng ngày bắt đầu")
            .When(x => x.AniversaryDate.HasValue && x.StartDate.HasValue);

        // Budget validation
        RuleFor(x => x.BudgetMin)
            .GreaterThanOrEqualTo(0).WithMessage("Ngân sách tối thiểu phải lớn hơn hoặc bằng 0")
            .When(x => x.BudgetMin.HasValue);

        RuleFor(x => x.BudgetMax)
            .GreaterThanOrEqualTo(0).WithMessage("Ngân sách tối đa phải lớn hơn hoặc bằng 0")
            .When(x => x.BudgetMax.HasValue);

        RuleFor(x => x.BudgetMax)
            .GreaterThanOrEqualTo(x => x.BudgetMin)
            .WithMessage("Ngân sách tối đa phải lớn hơn hoặc bằng ngân sách tối thiểu")
            .When(x => x.BudgetMin.HasValue && x.BudgetMax.HasValue);

        // Edge case: At least one field must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.CoupleName) || 
                      x.StartDate.HasValue || 
                      x.AniversaryDate.HasValue || 
                      x.BudgetMin.HasValue || 
                      x.BudgetMax.HasValue)
            .WithMessage("Phải cung cấp ít nhất một trường để cập nhật");
    }
}
