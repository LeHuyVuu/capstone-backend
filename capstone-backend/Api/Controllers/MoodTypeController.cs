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
}