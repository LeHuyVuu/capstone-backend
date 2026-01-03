using capstone_backend.Business.DTOs.User;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrEmpty(role) || role == "Admin" || role == "User")
            .WithMessage("Role phải là 'Admin' hoặc 'User'");
    }
}
