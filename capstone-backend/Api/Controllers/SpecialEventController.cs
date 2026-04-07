using capstone_backend.Business.DTOs.SpecialEvent;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SpecialEventController : BaseController
{
    private readonly ISpecialEventService _specialEventService;

    public SpecialEventController(ISpecialEventService specialEventService)
    {
        _specialEventService = specialEventService;
    }

    /// <summary>
    /// ADMIN ONLY - Create a new special event
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateSpecialEvent([FromBody] CreateSpecialEventRequest request)
    {
        var specialEvent = await _specialEventService.CreateSpecialEventAsync(request);
        return CreatedResponse(specialEvent, "Tạo sự kiện đặc biệt thành công");
    }

    /// <summary>
    /// ADMIN ONLY - Get special event by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSpecialEventById(int id)
    {
        var specialEvent = await _specialEventService.GetSpecialEventByIdAsync(id);
        if (specialEvent == null)
            return NotFoundResponse("Không tìm thấy sự kiện đặc biệt");

        return OkResponse(specialEvent);
    }

    /// <summary>
    /// Get all special events (paginated)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllSpecialEvents([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var events = await _specialEventService.GetAllSpecialEventsAsync(page, pageSize);
        return OkResponse(events);
    }

    /// <summary>
    /// Get currently active special events
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveSpecialEvents()
    {
        var events = await _specialEventService.GetActiveSpecialEventsAsync();
        return OkResponse(events);
    }

    /// <summary>
    /// ADMIN ONLY - Update special event
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateSpecialEvent(int id, [FromBody] UpdateSpecialEventRequest request)
    {
        var specialEvent = await _specialEventService.UpdateSpecialEventAsync(id, request);
        if (specialEvent == null)
            return NotFoundResponse("Không tìm thấy sự kiện đặc biệt");

        return OkResponse(specialEvent, "Cập nhật sự kiện đặc biệt thành công");
    }

    /// <summary>
    /// ADMIN ONLY - Patch (partial update) special event
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> PatchSpecialEvent(int id, [FromBody] UpdateSpecialEventRequest request)
    {
        // PATCH uses same DTO as PUT but fields are optional
        var specialEvent = await _specialEventService.UpdateSpecialEventAsync(id, request);
        if (specialEvent == null)
            return NotFoundResponse("Không tìm thấy sự kiện đặc biệt");

        return OkResponse(specialEvent, "Cập nhật một phần sự kiện đặc biệt thành công");
    }

    /// <summary>
    /// ADMIN ONLY - Delete special event
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteSpecialEvent(int id)
    {
        var result = await _specialEventService.DeleteSpecialEventAsync(id);
        if (!result)
            return NotFoundResponse("Không tìm thấy sự kiện đặc biệt");

        return OkResponse<object?>(null, "Xóa sự kiện đặc biệt thành công");
    }
}
