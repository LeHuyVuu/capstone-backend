using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MoodTypeController : BaseController
{
    private readonly IMoodTypeService _moodTypeService;

    public MoodTypeController(IMoodTypeService moodTypeService)
    {
        _moodTypeService = moodTypeService;
    }

    /// <summary>
    /// Get all active mood types
    /// </summary>
    /// <param name="gender">male | female (optional)</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllMoodTypes([FromQuery] string? gender)
    {
        var moodTypes = await _moodTypeService.GetAllMoodTypesAsync(gender);
        return OkResponse(moodTypes);
    }

    /// <summary>
    /// Get mood type by ID
    /// </summary>
    /// <param name="id">ID của mood type</param>
    /// <param name="gender">male | female (optional)</param>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMoodTypeById(int id, [FromQuery] string? gender)
    {
        var moodType = await _moodTypeService.GetMoodTypeByIdAsync(id, gender);
        if (moodType == null)
            return NotFoundResponse("Mood type not found");

        return OkResponse(moodType);
    }

    /// <summary>
    /// Cập nhật mood type vào member profile (user chọn từ danh sách mood type)
    /// </summary>
    /// <param name="request">Request chứa mood type ID</param>
    /// <returns>Thông tin mood type đã được cập nhật</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="400">Dữ liệu không hợp lệ</response>
    /// <response code="401">Chưa đăng nhập</response>
    /// <response code="404">Không tìm thấy mood type hoặc member profile</response>
    [HttpPost("update-mood")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMoodTypeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateMoodType([FromBody] UpdateMoodTypeRequest request)
    {
        // Lấy user ID từ token
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedResponse("Vui lòng đăng nhập để sử dụng tính năng này");
        }

        try
        {
            var result = await _moodTypeService.UpdateMoodTypeForUserAsync(userId.Value, request.MoodTypeId);

            if (result == null)
            {
                return NotFoundResponse("Không tìm thấy mood type hoặc member profile");
            }

            return OkResponse(result, "Cập nhật mood type thành công");
        }
        catch
        {
            return InternalServerErrorResponse("Có lỗi xảy ra khi cập nhật mood type. Vui lòng thử lại.");
        }
    }
}