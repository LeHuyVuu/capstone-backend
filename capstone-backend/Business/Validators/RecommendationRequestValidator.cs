using FluentValidation;
using capstone_backend.Business.DTOs.Recommendation;

namespace capstone_backend.Business.Validators;

/// <summary>
/// Flexible validator - accepts requests with minimal information
/// AI will handle recommendations even with incomplete data
/// </summary>
public class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        // Query validation - if provided, must not be empty
        When(x => x.Query != null, () =>
        {
            RuleFor(x => x.Query)
                .MaximumLength(500).WithMessage("Nội dung truy vấn không được vượt quá 500 ký tự");
        });

        // MBTI validation - if provided, must be 4 characters
        When(x => !string.IsNullOrEmpty(x.MbtiType), () =>
        {
            RuleFor(x => x.MbtiType)
                .Length(4).WithMessage("MbtiType phải có 4 ký tự (ví dụ: INTJ, ESFP)");
        });

        // Budget level validation - if provided, must be 1-3
        When(x => x.BudgetLevel.HasValue, () =>
        {
            RuleFor(x => x.BudgetLevel)
                .InclusiveBetween(1, 3).WithMessage("BudgetLevel phải là 1 (Thấp), 2 (Trung bình) hoặc 3 (Cao)");
        });
    }
}
