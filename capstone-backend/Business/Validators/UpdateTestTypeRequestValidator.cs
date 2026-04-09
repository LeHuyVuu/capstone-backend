using capstone_backend.Business.DTOs.TestType;
using FluentValidation;

namespace capstone_backend.Business.Validators
{
    public class UpdateTestTypeRequestValidator : AbstractValidator<UpdateTestTypeRequest>
    {
        public UpdateTestTypeRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name không được để trống")
                .MaximumLength(100).WithMessage("Name không được vượt quá 100 ký tự")
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description không được để trống")
                .MaximumLength(500).WithMessage("Description không được vượt quá 500 ký tự")
                .When(x => x.Description != null);

            RuleFor(x => x.TotalQuestions)
                .InclusiveBetween(1, 100)
                .WithMessage("TotalQuestions phải từ 1 đến 100")
                .When(x => x.TotalQuestions.HasValue);
        }
    }
}
