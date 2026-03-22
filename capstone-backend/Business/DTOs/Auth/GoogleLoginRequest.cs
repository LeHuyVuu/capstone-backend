namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request để login bằng Google (từ Flutter mobile)
/// </summary>
public class GoogleLoginRequest
{
    /// <summary>
    /// Google ID Token từ Google Sign-In
    /// </summary>
    public string IdToken { get; set; } = null!;
}
