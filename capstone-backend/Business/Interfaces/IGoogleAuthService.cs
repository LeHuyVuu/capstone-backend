using Google.Apis.Auth;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Interface cho Google Authentication Service
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Verify Google ID Token và trả về payload
    /// </summary>
    Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken);
}
