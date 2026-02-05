using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // Lấy danh sách users có phân trang
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _userService.GetUsersAsync(pageNumber, pageSize, searchTerm);
        return OkResponse(result);
    }

    // Get user by ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFoundResponse();
        return OkResponse(user);
    }

    // Create new user (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request, GetCurrentUserId());
            return CreatedResponse(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    // Update user
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request, GetCurrentUserId());
        if (user == null) return NotFoundResponse();
        return OkResponse(user);
    }

    // Delete user (soft delete, Admin only)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success) return NotFoundResponse();
        return OkResponse<object?>(null, "Deleted successfully");
    }

    // Update venue owner profile info (CCCD, Business License)
    [HttpPut("venue-owner/documents")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> UpdateDocumentVenueOwner([FromBody] UpdateDocumentVenueOwnerRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        try 
        {
            var user = await _userService.UpdateDocumentVenueOwnerAsync(userId.Value, request);
            if (user == null) return NotFoundResponse();
            return OkResponse(user);
        }
        catch (UnauthorizedAccessException ex)
        {
             return ForbiddenResponse(ex.Message);
        }
    }
}
