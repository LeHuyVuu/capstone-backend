using System.Security.Claims;
using capstone_backend.Api.Controllers;
using capstone_backend.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using capstone_backend.Api.Models;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Static;

namespace capstone_backend.Api.VenueRecommendation.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public class UserContextController : BaseController
{
    private readonly MyDbContext _dbContext;
    private readonly ILogger<UserContextController> _logger;

    public UserContextController(
        MyDbContext dbContext,
        ILogger<UserContextController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("user-context")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<IActionResult> GetUserContext()
    {
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

        var latestPersonalityResultCode = await _dbContext.PersonalityTests
            .AsNoTracking()
            .Where(p => p.MemberId == memberProfile.Id
                        && p.IsDeleted != true
                        && p.Status == PersonalityTestStatus.COMPLETED.ToString())
            .OrderByDescending(p => p.TakenAt ?? p.CreatedAt)
            .Select(p => p.ResultCode)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(latestPersonalityResultCode))
        {
            var mbtiInfo = MbtiContentStore.GetProfile(latestPersonalityResultCode);
            latestPersonalityResultCode = $"{mbtiInfo.Name} ({mbtiInfo.Code})";
        }
        
        var contextParts = new List<string>();

        if (latestInteraction != null)
        {
            var interactionText = "user likes";
            if (!string.IsNullOrWhiteSpace(latestInteraction.CategoryInteraction))
            {
                interactionText += $" category {latestInteraction.CategoryInteraction}";
            }

            contextParts.Add(interactionText);
        }

        if (latestMood != null)
        {
            var moodText = $"current mood {latestMood.MoodName}";
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

        if (!string.IsNullOrWhiteSpace(latestPersonalityResultCode))
        {
            contextParts.Add($"personality test {latestPersonalityResultCode}");
        }

        var userContext = string.Join(". ", contextParts);
        if (string.IsNullOrWhiteSpace(userContext))
        {
            userContext = "suggest popular and diverse venues";
            _logger.LogInformation("[USER CONTEXT] fallback default context for new member {MemberId}: {UserContext}", memberProfile.Id, userContext);
            return OkResponse(userContext, "New user context generated successfully");
        }

        _logger.LogInformation("[USER CONTEXT] userContext for member {MemberId}: {UserContext}", memberProfile.Id, userContext);

        return OkResponse(userContext, "User context retrieved successfully");
    }
}
