namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request để update password (yêu cầu password cũ)
/// </summary>
public class UpdatePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
