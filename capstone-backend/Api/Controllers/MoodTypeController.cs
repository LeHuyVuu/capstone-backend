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
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMoodTypeById(int id, [FromQuery] string? gender)
    {
        var moodType = await _moodTypeService.GetMoodTypeByIdAsync(id, gender);
        if (moodType == null)
            return NotFoundResponse("Mood type not found");

        return OkResponse(moodType);
    }

    [HttpPost("update-mood")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMoodTypeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateMoodType([FromBody] UpdateMoodTypeRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedResponse("Vui lÃ²ng Ä‘Äƒng nháº­p Ä‘á»ƒ sá»­ dá»¥ng tÃ­nh nÄƒng nÃ y");
        }

        try
        {
            var result = await _moodTypeService.UpdateMoodTypeForUserAsync(userId.Value, request.MoodTypeId);

            if (result == null)
            {
                return NotFoundResponse("KhÃ´ng tÃ¬m tháº¥y mood type hoáº·c member profile");
            }

            return OkResponse(result, "Cáº­p nháº­t mood type thÃ nh cÃ´ng");
        }
        catch
        {
            return InternalServerErrorResponse("Có lỗi xảy ra khi cập nhật mood type. Vui lòng thử lại.");
        }
    }


    [HttpGet("current-mood")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentMoodResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetCurrentMood()
    {
        // Lấy user ID từ token
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedResponse("Vui lòng đăng nhập để sử dụng tính năng này");
        }

        try
        {
            var result = await _moodTypeService.GetCurrentMoodAsync(userId.Value);

            if (result == null)
            {
                return NotFoundResponse("Không tìm thấy member profile");
            }

            return OkResponse(result, "Lấy tâm trạng hiện tại thành công");
        }
        catch
        {
            return InternalServerErrorResponse("Có lỗi xảy ra khi lấy tâm trạng. Vui lòng thử lại.");
        }
    }
}
