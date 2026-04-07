using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
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
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        var histories = await _searchHistoryService.GetSearchHistoriesByMemberAsync(userId.Value, page, pageSize);
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
            return ForbiddenResponse("Bạn không có quyền xem lịch sử tìm kiếm của thành viên này");

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
            return NotFoundResponse("Không tìm thấy lịch sử tìm kiếm hoặc bạn không có quyền xóa");

        return OkResponse<object?>(null, "Xóa lịch sử tìm kiếm thành công");
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
        return OkResponse<object?>(null, "Xóa toàn bộ lịch sử tìm kiếm thành công");
    }
}
