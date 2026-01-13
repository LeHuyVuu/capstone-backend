using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.Interfaces;
using Microsoft.Extensions.Options;

namespace capstone_backend.Business.Services;

public class RecommendationService : IRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        HttpClient httpClient,
        IOptions<OpenAISettings> settings,
        ILogger<RecommendationService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
    }

    public async Task<RecommendationResponse> GetRecommendationsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Tạo Thread
            var threadId = await CreateThreadAsync(query, cancellationToken);
            _logger.LogInformation("Created thread: {ThreadId}", threadId);

            // Step 2: Chạy Assistant
            var runId = await CreateRunAsync(threadId, cancellationToken);
            _logger.LogInformation("Created run: {RunId}", runId);

            // Step 3: Poll Run Status
            await WaitForRunCompletionAsync(threadId, runId, cancellationToken);
            _logger.LogInformation("Run completed: {RunId}", runId);

            // Step 4: Lấy Messages
            var recommendations = await GetMessagesAsync(threadId, cancellationToken);
            
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for query: {Query}", query);
            return new RecommendationResponse { Recommendations = new() };
        }
    }

    private async Task<string> CreateThreadAsync(string query, CancellationToken cancellationToken)
    {
        var payload = new
        {
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = query
                }
            }
        };

        var response = await _httpClient.PostAsync(
            "threads",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(content);
        
        return result.RootElement.GetProperty("id").GetString() 
            ?? throw new Exception("Thread ID not found");
    }

    private async Task<string> CreateRunAsync(string threadId, CancellationToken cancellationToken)
    {
        var payload = new
        {
            assistant_id = _settings.AssistantId
        };

        var response = await _httpClient.PostAsync(
            $"threads/{threadId}/runs",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(content);
        
        return result.RootElement.GetProperty("id").GetString() 
            ?? throw new Exception("Run ID not found");
    }

    private async Task WaitForRunCompletionAsync(
        string threadId, 
        string runId, 
        CancellationToken cancellationToken)
    {
        var delays = new[] { 300, 600, 1000, 1500, 2000, 2000 }; // Backoff polling
        var delayIndex = 0;
        var timeout = TimeSpan.FromSeconds(20);
        var startTime = DateTime.UtcNow;

        while (true)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                throw new TimeoutException("Run execution timeout");
            }

            var response = await _httpClient.GetAsync(
                $"threads/{threadId}/runs/{runId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonDocument.Parse(content);
            var status = result.RootElement.GetProperty("status").GetString();

            _logger.LogDebug("Run status: {Status}", status);

            if (status == "completed")
            {
                return;
            }

            if (status == "failed" || status == "cancelled" || status == "expired")
            {
                var errorMessage = "unknown error";
                if (result.RootElement.TryGetProperty("last_error", out var errorProp))
                {
                    errorMessage = errorProp.GetProperty("message").GetString() ?? errorMessage;
                }
                throw new Exception($"Run {status}: {errorMessage}");
            }

            // Queued or in_progress - wait and retry
            var delay = delayIndex < delays.Length 
                ? delays[delayIndex++] 
                : delays[^1];
            
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task<RecommendationResponse> GetMessagesAsync(
        string threadId, 
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"threads/{threadId}/messages?limit=10",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(content);

        // Lấy message mới nhất từ assistant
        var messages = result.RootElement.GetProperty("data");
        
        foreach (var message in messages.EnumerateArray())
        {
            var role = message.GetProperty("role").GetString();
            if (role == "assistant")
            {
                var contentArray = message.GetProperty("content");
                
                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    if (contentItem.GetProperty("type").GetString() == "text")
                    {
                        var text = contentItem.GetProperty("text").GetProperty("value").GetString();
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            return ParseRecommendationJson(text);
                        }
                    }
                }
            }
        }

        return new RecommendationResponse { Recommendations = new() };
    }

    private RecommendationResponse ParseRecommendationJson(string text)
    {
        try
        {
            // Try to parse as JSON directly
            var response = JsonSerializer.Deserialize<RecommendationResponse>(text, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (response != null && response.Recommendations.Any())
            {
                return response;
            }

            // Try to extract JSON from markdown code block
            var jsonStart = text.IndexOf('{');
            var jsonEnd = text.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonText = text.Substring(jsonStart, jsonEnd - jsonStart + 1);
                response = JsonSerializer.Deserialize<RecommendationResponse>(jsonText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (response != null)
                {
                    return response;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse recommendation JSON: {Text}", text);
        }

        return new RecommendationResponse { Recommendations = new() };
    }
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string AssistantId { get; set; } = string.Empty;
}
