namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request để verify OTP code
/// </summary>
public class VerifyOtpRequest
{
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;
}
