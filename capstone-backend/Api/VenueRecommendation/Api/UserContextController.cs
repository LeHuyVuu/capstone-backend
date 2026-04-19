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
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetUserContext()
    {
        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return UnauthorizedResponse("ID người dùng trong token không hợp lệ");
        }

        var memberProfile = await _dbContext.MemberProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDeleted != true);

        if (memberProfile == null)
        {
            return NotFoundResponse("Không tìm thấy hồ sơ thành viên");
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

        var searchHistories = latestInteraction?.CategoryInteraction;

        var latestPersonalityResultCode = await _dbContext.PersonalityTests
            .AsNoTracking()
            .Where(p => p.MemberId == memberProfile.Id
                        && p.IsDeleted != true
                        && p.Status == PersonalityTestStatus.COMPLETED.ToString())
            .OrderByDescending(p => p.TakenAt ?? p.CreatedAt)
            .Select(p => p.ResultCode)
            .FirstOrDefaultAsync();

        string? personalityDescription = null;
        if (!string.IsNullOrWhiteSpace(latestPersonalityResultCode))
        {
            var mbtiInfo = MbtiContentStore.GetProfile(latestPersonalityResultCode);
            if (!string.IsNullOrWhiteSpace(mbtiInfo.PersonalityDescription))
            {
                personalityDescription = mbtiInfo.PersonalityDescription;
            }
        }
        
        var contextParts = new List<string>();

        if (latestInteraction != null)
        {
            var interactionText = "user like to view venues";
            if (!string.IsNullOrWhiteSpace(latestInteraction.CategoryInteraction))
            {
                interactionText += $" category {latestInteraction.CategoryInteraction}";
            }

            contextParts.Add(interactionText);
        }

        if (!string.IsNullOrWhiteSpace(personalityDescription))
        {
            contextParts.Add(personalityDescription);
        }

        var userContext = string.Join(". user personality: ", contextParts);
        if (string.IsNullOrWhiteSpace(userContext))
        {
            userContext = "suggest popular and diverse venues";
            _logger.LogInformation("[USER CONTEXT] fallback default context for new member {MemberId}: {UserContext}", memberProfile.Id, userContext);
            return OkResponse(new
            {
                userContext,
                searchHistories
            }, "Tạo ngữ cảnh người dùng mới thành công");
        }

        _logger.LogInformation("[USER CONTEXT] userContext for member {MemberId}: {UserContext}", memberProfile.Id, userContext);

        return OkResponse(new
        {
            userContext,
            searchHistories
        }, "Lấy ngữ cảnh người dùng thành công");
    }
}
