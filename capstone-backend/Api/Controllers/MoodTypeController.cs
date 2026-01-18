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
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllMoodTypes()
    {
        var moodTypes = await _moodTypeService.GetAllMoodTypesAsync();
        return OkResponse(moodTypes);
    }

    /// <summary>
    /// Get mood type by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMoodTypeById(int id)
    {
        var moodType = await _moodTypeService.GetMoodTypeByIdAsync(id);
        if (moodType == null)
            return NotFoundResponse("Mood type not found");

        return OkResponse(moodType);
    }
}
