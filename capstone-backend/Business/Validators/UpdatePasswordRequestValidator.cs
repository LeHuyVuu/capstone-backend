using capstone_backend.Business.DTOs.Auth;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class UpdatePasswordRequestValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu mới phải có ít nhất 1 chữ hoa")
            .Matches(@"[a-z]").WithMessage("Mật khẩu mới phải có ít nhất 1 chữ thường")
            .Matches(@"[0-9]").WithMessage("Mật khẩu mới phải có ít nhất 1 chữ số")
            .Matches(@"[\W_]").WithMessage("Mật khẩu mới phải có ít nhất 1 ký tự đặc biệt");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống")
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp");

        RuleFor(x => x)
            .Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");
    }
}
