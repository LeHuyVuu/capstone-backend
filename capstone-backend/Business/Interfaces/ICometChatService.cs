namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface for CometChat integration
/// </summary>
public interface ICometChatService
{
    /// <summary>
    /// Create a new user in CometChat
    /// </summary>
    /// <param name="email">User's email (preferred for UID)</param>
    /// <param name="displayName">User's display name (fallback for UID if no email)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CometChat UID (usually "user_{email}" or "user_{name}")</returns>
    Task<string> CreateCometChatUserAsync(string email, string displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate CometChat auth token for a user
    /// </summary>
    /// <param name="cometChatUid">CometChat UID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CometChat auth token</returns>
    Task<string> GenerateCometChatAuthTokenAsync(string cometChatUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure CometChat user exists, create if not found
    /// </summary>
    /// <param name="email">User's email (preferred for UID)</param>
    /// <param name="displayName">User's display name (fallback for UID if no email)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CometChat UID</returns>
    Task<string> EnsureCometChatUserExistsAsync(string email, string displayName, CancellationToken cancellationToken = default);
}
