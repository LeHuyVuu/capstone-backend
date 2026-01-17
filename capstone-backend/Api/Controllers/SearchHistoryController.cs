using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/v1/[controller]")]
[Authorize]
public class SearchHistoryController : BaseController
{
    private readonly ISearchHistoryService _searchHistoryService;

    public SearchHistoryController(ISearchHistoryService searchHistoryService)
    {
        _searchHistoryService = searchHistoryService;
    }

    /// <summary>
    /// Get all search histories for current member (paginated)
    /// </summary>
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMySearchHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var histories = await _searchHistoryService.GetSearchHistoriesByMemberAsync(memberId.Value, page, pageSize);
        return OkResponse(histories);
    }

    /// <summary>
    /// Get search histories by member ID (for admin or specific use)
    /// </summary>
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetSearchHistoriesByMember(int memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return UnauthorizedResponse();

        // Only allow users to see their own history or admin can see anyone's
        if (currentUserId != memberId && !User.IsInRole("admin"))
            return ForbiddenResponse("You don't have permission to view this member's search history");

        var histories = await _searchHistoryService.GetSearchHistoriesByMemberAsync(memberId, page, pageSize);
        return OkResponse(histories);
    }

    /// <summary>
    /// Delete a specific search history entry
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSearchHistory(int id)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var result = await _searchHistoryService.DeleteSearchHistoryAsync(id, memberId.Value);
        if (!result)
            return NotFoundResponse("Search history not found or you don't have permission to delete it");

        return OkResponse<object?>(null, "Search history deleted successfully");
    }

    /// <summary>
    /// Clear all search history for current member
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearSearchHistory()
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        await _searchHistoryService.ClearSearchHistoryAsync(memberId.Value);
        return OkResponse<object?>(null, "Search history cleared successfully");
    }
}
