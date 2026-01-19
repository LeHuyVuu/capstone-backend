using capstone_backend.Business.DTOs.Auth;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(255).WithMessage("Email không được quá 255 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được quá 100 ký tự");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu là bắt buộc")
            .Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(100).WithMessage("Họ tên không được quá 100 ký tự");

        RuleFor(x => x.Gender)
            .Must(gender => gender == null || gender == "MALE" || gender == "FEMALE" || gender == "OTHER")
            .WithMessage("Giới tính phải là 'MALE', 'FEMALE' hoặc 'OTHER'")
            .When(x => !string.IsNullOrEmpty(x.Gender));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
