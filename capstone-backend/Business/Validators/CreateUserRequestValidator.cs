using capstone_backend.Business.DTOs.User;
using FluentValidation;

namespace capstone_backend.Business.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không hợp lệ")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password không được để trống")
            .MinimumLength(6).WithMessage("Password phải ít nhất 6 ký tự")
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role không được để trống")
            .Must(role => string.Equals(role?.Trim(), "STAFF", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Endpoint này chỉ hỗ trợ tạo role STAFF");

        RuleFor(x => x.LocationId)
            .NotNull().WithMessage("LocationId là bắt buộc khi role là STAFF")
            .GreaterThan(0).WithMessage("LocationId phải lớn hơn 0")
            .When(x => string.Equals(x.Role?.Trim(), "STAFF", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.LocationId)
            .Null().WithMessage("LocationId chỉ được dùng khi role là STAFF")
            .When(x => !string.Equals(x.Role?.Trim(), "STAFF", StringComparison.OrdinalIgnoreCase));
    }
}
