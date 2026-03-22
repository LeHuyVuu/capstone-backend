namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request để reset password sau khi verify OTP thành công
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
