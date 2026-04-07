using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;


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

    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    protected bool IsCurrentUserInRole(string role)
    {
        var userRole = GetCurrentUserRole();
        return string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase);
    }

    // Responses thành công
    protected IActionResult OkResponse<T>(T data, string message = "Thành công")
        => Ok(ApiResponse<T>.Success(data, message, 200, GetTraceId()));

    protected IActionResult OkResponse(string message = "Thành công")
        => Ok(ApiResponse<object>.Success(null, message, 200, GetTraceId()));

    protected IActionResult CreatedResponse<T>(T data, string message = "Đã tạo")
        => StatusCode(201, ApiResponse<T>.Success(data, message, 201, GetTraceId()));

    // Responses lỗi
    protected IActionResult BadRequestResponse(string message = "Yêu cầu không hợp lệ")
        => BadRequest(ApiResponse<object>.Error(message, 400, GetTraceId()));

    protected IActionResult BadRequestResponse<T>(T data, string message = "Yêu cầu không hợp lệ")
        => BadRequest(ApiResponse<object>.ErrorData(data, message, 400, GetTraceId()));

    protected IActionResult NotFoundResponse(string message = "Không tìm thấy dữ liệu")
        => NotFound(ApiResponse<object>.Error(message, 404, GetTraceId()));

    protected IActionResult UnauthorizedResponse(string message = "Không có quyền truy cập")
        => Unauthorized(ApiResponse<object>.Error(message, 401, GetTraceId()));

    protected IActionResult ForbiddenResponse(string message = "Bị từ chối truy cập")
        => StatusCode(403, ApiResponse<object>.Error(message, 403, GetTraceId()));

    protected IActionResult InternalServerErrorResponse(string message = "Lỗi máy chủ nội bộ")
        => StatusCode(500, ApiResponse<object>.Error(message, 500, GetTraceId()));
}
