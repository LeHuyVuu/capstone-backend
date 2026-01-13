using FluentValidation;
using capstone_backend.Business.DTOs.Recommendation;

namespace capstone_backend.Business.Validators;

public class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query không được để trống")
            .MaximumLength(1000).WithMessage("Query không được vượt quá 1000 ký tự");
    }
}
