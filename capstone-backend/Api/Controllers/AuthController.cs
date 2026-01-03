using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[Route("api/v1/[controller]")]
public class AuthController : BaseController
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    // Đăng nhập bằng email và password
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var loginResponse = await _userService.LoginAsync(request);

        if (loginResponse == null)
            return UnauthorizedResponse("Đăng nhập thất bại");

        // Tạo claims cho user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, loginResponse.UserId.ToString()),
            new Claim(ClaimTypes.Email, loginResponse.Email),
            new Claim(ClaimTypes.Name, loginResponse.FullName),
            new Claim(ClaimTypes.Role, loginResponse.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in với cookie
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            });

        return OkResponse(loginResponse, "Đăng nhập thành công");
    }

    // Đăng xuất
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return OkResponse<object?>(null, "Đăng xuất thành công");
    }

    // Lấy thông tin user hiện tại
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var user = await _userService.GetCurrentUserAsync(userId.Value);
        if (user == null) return NotFoundResponse();

        return OkResponse(user);
    }
}
