using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

// Base controller cho tất cả các controllers
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected string GetTraceId() => HttpContext.Items["TraceId"]?.ToString() ?? HttpContext.TraceIdentifier;
    protected int? GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userId, out var id) ? id : null;
    }

    // Responses thành công
    protected IActionResult OkResponse<T>(T data, string message = "Success")
        => Ok(ApiResponse<T>.Success(data, message, 200, GetTraceId()));

    protected IActionResult CreatedResponse<T>(T data, string message = "Created")
        => StatusCode(201, ApiResponse<T>.Success(data, message, 201, GetTraceId()));

    // Responses lỗi
    protected IActionResult BadRequestResponse(string message = "Bad request")
        => BadRequest(ApiResponse<object>.Error(message, 400, GetTraceId()));

    protected IActionResult NotFoundResponse(string message = "Not found")
        => NotFound(ApiResponse<object>.Error(message, 404, GetTraceId()));

    protected IActionResult UnauthorizedResponse(string message = "Unauthorized")
        => Unauthorized(ApiResponse<object>.Error(message, 401, GetTraceId()));
}
