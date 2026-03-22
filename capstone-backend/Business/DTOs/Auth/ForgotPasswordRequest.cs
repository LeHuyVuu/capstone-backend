namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request để gửi OTP qua email khi quên mật khẩu
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}
