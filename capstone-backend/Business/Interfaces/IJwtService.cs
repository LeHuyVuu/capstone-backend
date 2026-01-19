namespace capstone_backend.Business.Interfaces;

/// <summary>
/// JWT Token service for generating and validating tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate access token for user
    /// </summary>
    string GenerateAccessToken(int userId, string email, string role, string fullName);

    /// <summary>
    /// Generate refresh token for user
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate and get user ID from token
    /// </summary>
    int? ValidateToken(string token);
}
