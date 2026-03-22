using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderboardController : BaseController
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Get Leaderboard by Year and Month
    /// </summary>
    /// <param name="year">Năm (ví dụ: 2026)</param>
    /// <param name="month">Tháng (1-12)</param>
    /// <param name="pageNumber">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng mỗi trang (mặc định: 50)</param>
    /// <returns>Bảng xếp hạng theo tháng</returns>
    /// <remarks>
    /// Ví dụ: GET /api/Leaderboard?year=2026&amp;month=3
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _leaderboardService.GetMonthlyLeaderboardAsync(year, month, pageNumber, pageSize);
            
            if (result == null)
                return NotFoundResponse($"Không tìm thấy bảng xếp hạng tháng {month}/{year}");

            return OkResponse(result, "Lấy bảng xếp hạng thành công");
        }
        catch (ArgumentException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse(ex.Message);
        }
    }
}
