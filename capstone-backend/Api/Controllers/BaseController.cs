using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected string GetTraceId() => HttpContext.Items["TraceId"]?.ToString() ?? HttpContext.TraceIdentifier;
    
    protected int? GetCurrentUserId()
    {
        // Try to get from JWT token (Sub claim or NameIdentifier)
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return int.TryParse(userIdClaim, out var id) ? id : null;
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

    protected IActionResult InternalServerErrorResponse(string message = "Internal server error")
        => StatusCode(500, ApiResponse<object>.Error(message, 500, GetTraceId()));
}
