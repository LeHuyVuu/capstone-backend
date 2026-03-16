using System.Text;
using System.Text.Json;
using System.Security.Claims;
using capstone_backend.Api.Controllers;
using capstone_backend.Api.Models;
using capstone_backend.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using capstone_backend.Api.VenueRecommendation.Api.DTOs;
using capstone_backend.Business.DTOs.Common;
using Newtonsoft.Json;

namespace capstone_backend.Api.VenueRecommendation.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public class MeilisearchPersonalizeController : BaseController
{
    private readonly MyDbContext _dbContext;
    private readonly ILogger<MeilisearchPersonalizeController> _logger;

    public MeilisearchPersonalizeController(
        MyDbContext dbContext,
        ILogger<MeilisearchPersonalizeController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("personalized")]
    [ProducesResponseType(typeof(ApiResponse<VenueLocationQueryResponse>), 200)]
    public async Task<IActionResult> PersonalizedSearch([FromBody] MeilisearchPersonalizeTestRequest? request)
    {
        request ??= new MeilisearchPersonalizeTestRequest();

        var host =  Environment.GetEnvironmentVariable("MEILISEARCH_HOST")
                   ?? "http://localhost:7700"; // Sử dụng localhost thay vì external IP

        var apiKey =  Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                    ?? string.Empty;

        _logger.LogInformation("[MEILI TEST] Using Meilisearch host: {Host}", host);
        _logger.LogInformation("[MEILI TEST] API key configured: {HasApiKey}", !string.IsNullOrWhiteSpace(apiKey));

        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return UnauthorizedResponse("Invalid token user id");
        }

        var memberProfile = await _dbContext.MemberProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDeleted != true);

        if (memberProfile == null)
        {
            return NotFoundResponse("Member profile not found");
        }

        var latestInteraction = await _dbContext.Interactions
            .AsNoTracking()
            .Where(i => i.MemberId == memberProfile.Id
                        && i.InteractionType == "VIEW"
                        && i.TargetType == "VenueLocation")
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.InteractionType,
                i.TargetType,
                i.CategoryInteraction,
                i.CreatedAt
            })
            .FirstOrDefaultAsync();

        var latestMood = await _dbContext.MemberMoodLogs
            .AsNoTracking()
            .Where(m => m.MemberId == memberProfile.Id && m.IsDeleted != true)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                MoodName = m.MoodType.Name,
                m.Reason,
                m.Note,
                m.CreatedAt
            })
            .FirstOrDefaultAsync();

        var contextParts = new List<string>();

        if (latestInteraction != null)
        {
            var interactionText = "latest viewed venue location";
            if (!string.IsNullOrWhiteSpace(latestInteraction.CategoryInteraction))
            {
                interactionText += $" in category {latestInteraction.CategoryInteraction}";
            }

            contextParts.Add(interactionText);
        }

        if (latestMood != null)
        {
            var moodText = $"latest mood {latestMood.MoodName}";
            if (!string.IsNullOrWhiteSpace(latestMood.Reason))
            {
                moodText += $", reason {latestMood.Reason}";
            }

            if (!string.IsNullOrWhiteSpace(latestMood.Note))
            {
                moodText += $", note {latestMood.Note}";
            }

            contextParts.Add(moodText);
        }

        var userContext = string.Join(". ", contextParts);
        _logger.LogInformation("[MEILI TEST] userContext for member {MemberId}: {UserContext}", memberProfile.Id, userContext);

        if (string.IsNullOrWhiteSpace(userContext))
        {
            return BadRequestResponse("Cannot build user context from interactions or mood");
        }

        if (string.IsNullOrWhiteSpace(request.IndexUid))
        {
            return BadRequestResponse("IndexUid is required");
        }

        var body = new Dictionary<string, object?>
        {
            ["q"] = request.Q,
            ["offset"] = request.Offset,
            ["limit"] = request.Limit,
            ["filter"] = request.Filter,
            ["sort"] = request.Sort,
            ["attributesToRetrieve"] = new[] { "*" },
            ["personalize"] = new
            {
                userContext
            }
        };

        if (request.UseHybrid)
        {
            body["hybrid"] = new
            {
                embedder = "venue-ai",
                semanticRatio = request.SemanticRatio
            };
        }

        // Retry logic với timeout ngắn hơn
        var maxRetries = 2;
        var retryDelay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10); // Timeout ngắn hơn
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }

                var json = System.Text.Json.JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[MEILI TEST] Attempt {Attempt}/{MaxAttempts} - Calling Meilisearch: {Url}", 
                    attempt + 1, maxRetries + 1, $"{host.TrimEnd('/')}/indexes/{request.IndexUid}/search");

                using var response = await httpClient.PostAsync($"{host.TrimEnd('/')}/indexes/{request.IndexUid}/search", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[MEILI TEST] Personalized search failed with status {StatusCode}: {ResponseBody}", (int)response.StatusCode, responseBody);
                    
                    // Nếu là lỗi client (4xx), không retry
                    if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                    {
                        return StatusCode(
                            (int)response.StatusCode,
                            ApiResponse<object>.ErrorData(responseBody, "Meilisearch personalized search failed", (int)response.StatusCode, GetTraceId()));
                    }
                    
                    // Nếu là lỗi server (5xx) và còn retry, thử lại
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("[MEILI TEST] Server error, retrying in {Delay}ms...", retryDelay.TotalMilliseconds);
                        await Task.Delay(retryDelay);
                        continue;
                    }
                    
                    return StatusCode(
                        (int)response.StatusCode,
                        ApiResponse<object>.ErrorData(responseBody, "Meilisearch personalized search failed", (int)response.StatusCode, GetTraceId()));
                }

                var meilisearchResponse = JsonConvert.DeserializeObject<MeilisearchSearchResponse<VenueLocationQueryResult>>(responseBody);
                if (meilisearchResponse == null)
                {
                    return InternalServerErrorResponse("Invalid Meilisearch response");
                }

                var hits = meilisearchResponse.Hits ?? new List<VenueLocationQueryResult>();
                var totalCount = meilisearchResponse.TotalHits
                                 ?? meilisearchResponse.EstimatedTotalHits
                                 ?? hits.Count;
                var pageNumber = request.Limit > 0 ? (request.Offset / request.Limit) + 1 : 1;
                var pageSize = request.Limit > 0 ? request.Limit : 20;

                var result = new VenueLocationQueryResponse
                {
                    Recommendations = new PagedResult<VenueLocationQueryResult>(hits, pageNumber, pageSize, totalCount),
                    Explanation = "Personalized search results based on the latest viewed venue category and latest mood.",
                    ProcessingTimeMs = meilisearchResponse.ProcessingTimeMs,
                    Query = request.Q,
                    PersonalityTags = !string.IsNullOrWhiteSpace(latestInteraction?.CategoryInteraction)
                        ? new List<string> { latestInteraction.CategoryInteraction }
                        : null,
                    CoupleMoodType = latestMood?.MoodName
                };

                _logger.LogInformation("[MEILI TEST] Success on attempt {Attempt} - Found {Count} venues in {ProcessingTime}ms", 
                    attempt + 1, result.Recommendations.TotalCount, result.ProcessingTimeMs);

                return OkResponse(result, $"Found {result.Recommendations.TotalCount} venues in {result.ProcessingTimeMs}ms");
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("[MEILI TEST] Attempt {Attempt} timeout: {Message}", attempt + 1, ex.Message);
                
                if (attempt < maxRetries)
                {
                    _logger.LogWarning("[MEILI TEST] Retrying in {Delay}ms...", retryDelay.TotalMilliseconds);
                    await Task.Delay(retryDelay);
                    continue;
                }
                
                return StatusCode(504, ApiResponse<object>.ErrorData("Request timeout after retries", "Meilisearch request timed out", 504, GetTraceId()));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("[MEILI TEST] Attempt {Attempt} HTTP error: {Message}", attempt + 1, ex.Message);
                
                if (attempt < maxRetries)
                {
                    _logger.LogWarning("[MEILI TEST] Retrying in {Delay}ms...", retryDelay.TotalMilliseconds);
                    await Task.Delay(retryDelay);
                    continue;
                }
                
                return StatusCode(502, ApiResponse<object>.ErrorData(ex.Message, "Failed to connect to Meilisearch after retries", 502, GetTraceId()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MEILI TEST] Unexpected error on attempt {Attempt}", attempt + 1);
                
                if (attempt < maxRetries)
                {
                    _logger.LogWarning("[MEILI TEST] Retrying in {Delay}ms...", retryDelay.TotalMilliseconds);
                    await Task.Delay(retryDelay);
                    continue;
                }
                
                return InternalServerErrorResponse("An unexpected error occurred during search after retries");
            }
        }

        return InternalServerErrorResponse("All retry attempts failed");
    }

    private sealed class MeilisearchSearchResponse<T>
    {
        [JsonProperty("hits")]
        public List<T>? Hits { get; set; }

        [JsonProperty("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        [JsonProperty("totalHits")]
        public int? TotalHits { get; set; }

        [JsonProperty("estimatedTotalHits")]
        public int? EstimatedTotalHits { get; set; }
    }
}
