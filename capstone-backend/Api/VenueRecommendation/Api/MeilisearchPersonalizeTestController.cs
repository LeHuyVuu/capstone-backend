using System.Text;
using System.Text.Json;
using System.Security.Claims;
using capstone_backend.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using capstone_backend.Api.VenueRecommendation.Api.DTOs;

namespace capstone_backend.Api.VenueRecommendation.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public class MeilisearchPersonalizeController : ControllerBase
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
    public async Task<IActionResult> PersonalizedSearch([FromBody] MeilisearchPersonalizeTestRequest? request)
    {
        request ??= new MeilisearchPersonalizeTestRequest();

        var host = Environment.GetEnvironmentVariable("MEILISEARCH_HOST_TEST")
                   ?? Environment.GetEnvironmentVariable("MEILISEARCH_HOST")
                   ?? "http://134.209.108.208:7700";

        var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY_TEST")
                    ?? Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                    ?? string.Empty;

        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "invalid token user id" });
        }

        var memberProfile = await _dbContext.MemberProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDeleted != true);

        if (memberProfile == null)
        {
            return NotFound(new { error = "member profile not found" });
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
            return BadRequest(new { error = "cannot build user context from interactions or mood" });
        }

        if (string.IsNullOrWhiteSpace(request.IndexUid))
        {
            return BadRequest(new { error = "indexUid is required" });
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

        using var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync($"{host.TrimEnd('/')}/indexes/{request.IndexUid}/search", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        return Content(responseBody, "application/json");
    }
}
