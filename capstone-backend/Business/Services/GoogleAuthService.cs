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
    private readonly string? _googleMobileClientId;

    public GoogleAuthService(ILogger<GoogleAuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                         ?? configuration["Google:ClientId"];
        _googleMobileClientId = Environment.GetEnvironmentVariable("GOOGLE_MOBILE_CLIENT_ID")
                                ?? configuration["Google:MobileClientId"];
        
        if (string.IsNullOrEmpty(_googleClientId))
        {
            _logger.LogWarning("Google Client ID not configured. Google login will not work.");
        }

        if (string.IsNullOrEmpty(_googleMobileClientId))
        {
            _logger.LogWarning("Google Mobile Client ID not configured. Mobile Google login will fallback to GOOGLE_CLIENT_ID.");
        }
    }

    /// <summary>
    /// Verify Google ID Token và trả về thông tin user
    /// </summary>
    public async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
    {
        return await VerifyGoogleTokenByAudienceAsync(idToken, _googleClientId, "web");
    }

    /// <summary>
    /// Verify Google ID Token cho mobile client và trả về thông tin user
    /// </summary>
    public async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleMobileTokenAsync(string idToken)
    {
        var mobileAudience = _googleMobileClientId ?? _googleClientId;
        return await VerifyGoogleTokenByAudienceAsync(idToken, mobileAudience, "mobile");
    }

    private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenByAudienceAsync(
        string idToken,
        string? audience,
        string channel)
    {
        try
        {
            if (string.IsNullOrEmpty(audience))
            {
                _logger.LogError("Google Client ID is not configured for channel: {Channel}", channel);
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { audience }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            
            _logger.LogInformation("Google token verified successfully for channel {Channel}, email: {Email}", channel, payload.Email);
            return payload;
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Invalid Google ID Token for channel: {Channel}", channel);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token for channel: {Channel}", channel);
            return null;
        }
    }
}
