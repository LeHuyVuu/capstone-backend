using capstone_backend.Business.DTOs.User;

namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Response model after successful login
/// </summary>
public class LoginResponse
{
  

    /// <summary>
    /// JWT Access Token (for API authorization)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token (to get new access token)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiry time in UTC
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    public string Gender { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? InviteCode { get; set; }

    public decimal Balance { get; set; }
    public int Points { get; set; }

    public int? LocationId { get; set; }
}
