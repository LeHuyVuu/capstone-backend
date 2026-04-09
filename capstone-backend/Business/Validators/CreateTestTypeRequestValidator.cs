using capstone_backend.Business.DTOs.TestType;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class CreateTestTypeRequestValidator : AbstractValidator<CreateTestTypeResquest>
    {
        public CreateTestTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("TestType name is required")
                .MaximumLength(100).WithMessage("TestType name can not exceed 100 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description can not exceed 500 characters");

            RuleFor(x => x.TotalQuestions)
                .InclusiveBetween(1, 100)
                .WithMessage("TotalQuestions must be between 1 and 100");
        }
    }
}
