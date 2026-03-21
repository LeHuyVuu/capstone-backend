using capstone_backend.Business.DTOs.Auth;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(255).WithMessage("Email không được vượt quá 255 ký tự");
    }
}
