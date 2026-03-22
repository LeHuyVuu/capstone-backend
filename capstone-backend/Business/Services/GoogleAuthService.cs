using Google.Apis.Auth;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service để verify Google ID Token
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string? _googleClientId;

    public GoogleAuthService(ILogger<GoogleAuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                         ?? configuration["Google:ClientId"];
        
        if (string.IsNullOrEmpty(_googleClientId))
        {
            _logger.LogWarning("Google Client ID not configured. Google login will not work.");
        }
    }

    /// <summary>
    /// Verify Google ID Token và trả về thông tin user
    /// </summary>
    public async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_googleClientId))
            {
                _logger.LogError("Google Client ID is not configured");
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            
            _logger.LogInformation("Google token verified successfully for email: {Email}", payload.Email);
            return payload;
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Invalid Google ID Token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token");
            return null;
        }
    }
}
