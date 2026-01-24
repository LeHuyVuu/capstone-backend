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
                .MaximumLength(500).WithMessage("Query must not exceed 500 characters");
        });

        // MBTI validation - if provided, must be 4 characters
        When(x => !string.IsNullOrEmpty(x.MbtiType), () =>
        {
            RuleFor(x => x.MbtiType)
                .Length(4).WithMessage("MbtiType must be 4 characters (e.g., INTJ, ESFP)");
        });

        // Limit validation - always required, default is 10
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20).WithMessage("Limit must be between 1 and 20");
        
        // Budget level validation - if provided, must be 1-3
        When(x => x.BudgetLevel.HasValue, () =>
        {
            RuleFor(x => x.BudgetLevel)
                .InclusiveBetween(1, 3).WithMessage("BudgetLevel must be 1 (Low), 2 (Medium), or 3 (High)");
        });
    }
}
