namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Response model after successful login
/// </summary>
/// <remarks>
/// Returns user information after authentication.
/// For cookie-based auth, session is stored in cookie, not in response body.
/// </remarks>
public class LoginResponse
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's role
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
