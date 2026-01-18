using capstone_backend.Business.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for integrating with CometChat REST API
/// </summary>
public class CometChatService : ICometChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CometChatService> _logger;

    private string CometChatAppId => Environment.GetEnvironmentVariable("COMETCHAT_APP_ID") 
        ?? throw new InvalidOperationException("COMETCHAT_APP_ID environment variable not configured");
    private string CometChatApiKey => Environment.GetEnvironmentVariable("COMETCHAT_API_KEY") 
        ?? throw new InvalidOperationException("COMETCHAT_API_KEY environment variable not configured");
    private string CometChatRestApiUrl => Environment.GetEnvironmentVariable("COMETCHAT_REST_API_URL") 
        ?? "https://api-us.cometchat.io/v3";

    public CometChatService(
        IHttpClientFactory httpClientFactory,
        ILogger<CometChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CreateCometChatUserAsync(string email, string displayName, CancellationToken cancellationToken = default)
    {
        // Use email if available, otherwise use displayName, sanitize for UID
        var identifier = !string.IsNullOrWhiteSpace(email) ? email : displayName;
        var cometChatUid = $"user_{SanitizeForUid(identifier)}";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("apiKey", CometChatApiKey);
            httpClient.DefaultRequestHeaders.Add("appId", CometChatAppId);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var createUserPayload = new
            {
                uid = cometChatUid,
                name = displayName
            };

            var jsonContent = JsonSerializer.Serialize(createUserPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                $"{CometChatRestApiUrl}/users",
                content,
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Created CometChat user: {CometChatUid}", cometChatUid);
                return cometChatUid;
            }

            // If user already exists (409 Conflict), that's okay - return the UID
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation("CometChat user already exists: {CometChatUid}", cometChatUid);
                return cometChatUid;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create CometChat user. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            throw new Exception($"Failed to create CometChat user: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error creating CometChat user: {CometChatUid}", cometChatUid);
            throw;
        }
        catch (Exception ex) when (ex.Message.Contains("already exists"))
        {
            // User already exists, that's fine
            _logger.LogInformation("CometChat user already exists: {CometChatUid}", cometChatUid);
            return cometChatUid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CometChat user: {CometChatUid}", cometChatUid);
            throw;
        }
    }

    public async Task<string> GenerateCometChatAuthTokenAsync(string cometChatUid, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("apiKey", CometChatApiKey);
            httpClient.DefaultRequestHeaders.Add("appId", CometChatAppId);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync(
                $"{CometChatRestApiUrl}/users/{cometChatUid}/auth_tokens",
                null,
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonDoc = JsonDocument.Parse(responseContent);
                
                // CometChat typically returns: { "data": { "authToken": "xxx" } }
                if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("authToken", out var authTokenElement))
                {
                    var authToken = authTokenElement.GetString() ?? throw new Exception("Auth token is null");
                    _logger.LogInformation("Generated CometChat auth token for: {CometChatUid}", cometChatUid);
                    return authToken;
                }

                throw new Exception("Unexpected CometChat API response format");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to generate CometChat auth token. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new Exception($"Failed to generate CometChat auth token: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CometChat auth token for: {CometChatUid}", cometChatUid);
            throw;
        }
    }

    public async Task<string> EnsureCometChatUserExistsAsync(string email, string displayName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to create the user (if already exists, will return 409 which we handle)
            return await CreateCometChatUserAsync(email, displayName, cancellationToken);
        }
        catch (Exception ex)
        {
            var identifier = !string.IsNullOrWhiteSpace(email) ? email : displayName;
            _logger.LogError(ex, "Error ensuring CometChat user exists: {Identifier}", identifier);
            throw;
        }
    }

    /// <summary>
    /// Sanitize string to be used as CometChat UID (remove special characters, spaces, etc.)
    /// </summary>
    private string SanitizeForUid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unknown";

        // Remove spaces, @, dots, and special characters - keep only alphanumeric and underscores
        var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
        
        // Limit length to 100 characters (CometChat UID limit)
        if (sanitized.Length > 100)
            sanitized = sanitized.Substring(0, 100);
            
        return sanitized.ToLowerInvariant();
    }
}
