using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var loginResponse = await _userService.LoginAsync(request);

        if (loginResponse == null)
            return UnauthorizedResponse("Invalid email or password");

        // Decode JWT token to get user claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(loginResponse.AccessToken);
        
        // Create claims for cookie authentication from JWT token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.First(c => c.Type == "sub").Value),
            new Claim(ClaimTypes.Email, jwtToken.Claims.First(c => c.Type == "email").Value),
            new Claim(ClaimTypes.Name, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value),
            new Claim(ClaimTypes.Role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in with cookie (for web)
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = request.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            });

        // Return full response with JWT tokens (for mobile) and user info (for web)
        return OkResponse(loginResponse, "Login successful");
    }


    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var registerResponse = await _userService.RegisterAsync(request);

            // Decode JWT token to get user claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(registerResponse.AccessToken);
            
            // Create claims for cookie authentication from JWT token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.First(c => c.Type == "sub").Value),
                new Claim(ClaimTypes.Email, jwtToken.Claims.First(c => c.Type == "email").Value),
                new Claim(ClaimTypes.Name, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value),
                new Claim(ClaimTypes.Role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie (for web)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            // Return full response with JWT tokens (for mobile) and user info (for web)
            return OkResponse(registerResponse, "Registration successful");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Registration failed: {ex.Message}");
        }
    }


    [HttpPost("register-venue-owner")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterVenueOwner([FromBody] RegisterVenueOwnerRequest request)
    {
        try
        {
            var registerResponse = await _userService.RegisterVenueOwnerAsync(request);

            // Decode JWT token to get user claims
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(registerResponse.AccessToken);
            
            // Create claims for cookie authentication from JWT token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, jwtToken.Claims.First(c => c.Type == "sub").Value),
                new Claim(ClaimTypes.Email, jwtToken.Claims.First(c => c.Type == "email").Value),
                new Claim(ClaimTypes.Name, jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value),
                new Claim(ClaimTypes.Role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie (for web)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            // Return full response with JWT tokens (for mobile) and user info (for web)
            return OkResponse(registerResponse, "VenueOwner registration successful");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"VenueOwner registration failed: {ex.Message}");
        }
    }


    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + "Bearer")]
    public async Task<IActionResult> Logout()
    {
        // Clear cookie if using cookie authentication (web)
        if (User.Identity?.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        
        // For JWT (mobile), client will remove tokens from storage
        // TODO: Implement token blacklist if needed
        
        return OkResponse<object?>(null, "Logout successful");
    }


    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + "Bearer")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var user = await _userService.GetCurrentUserAsync(userId.Value);
        if (user == null) return NotFoundResponse();

        return OkResponse(user);
    }

    /// <summary>
    /// Update password cho user đã đăng nhập
    /// </summary>
    /// <param name="request">Update password request</param>
    /// <returns>Success response</returns>
    [HttpPost("update-password")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + "Bearer")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return UnauthorizedResponse();

            await _userService.UpdatePasswordAsync(userId.Value, request);
            return OkResponse<object?>(null, "Cập nhật mật khẩu thành công");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Cập nhật mật khẩu thất bại: {ex.Message}");
        }
    }


    /// <summary>
    /// Gửi OTP qua email để reset password
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success response</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _userService.SendPasswordResetOtpAsync(request);
            return OkResponse<object?>(null, "Mã OTP đã được gửi đến email của bạn");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Gửi OTP thất bại: {ex.Message}");
        }
    }


    /// <summary>
    /// Verify OTP code
    /// </summary>
    /// <param name="request">Verify OTP request</param>
    /// <returns>Success response</returns>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            await _userService.VerifyOtpAsync(request);
            return OkResponse<object?>(null, "Xác thực OTP thành công");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Xác thực OTP thất bại: {ex.Message}");
        }
    }


    /// <summary>
    /// Reset password sau khi verify OTP thành công
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Success response</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request);
            return OkResponse<object?>(null, "Đặt lại mật khẩu thành công");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerErrorResponse($"Đặt lại mật khẩu thất bại: {ex.Message}");
        }
    }
}
