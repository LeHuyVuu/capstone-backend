using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, IUnitOfWork unitOfWork, ILogger<AuthController> logger)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var loginResponse = await _userService.LoginAsync(request);

        if (loginResponse == null)
            return UnauthorizedResponse("Invalid email or password");

        var staffVenueGuardResult = await ValidateStaffVenueAccessAsync(loginResponse.AccessToken);
        if (staffVenueGuardResult != null)
            return staffVenueGuardResult;

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

            // Validate request
            if (request == null)
                return BadRequestResponse("Request body không được để trống");

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequestResponse("Mật khẩu hiện tại không được để trống");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequestResponse("Mật khẩu mới không được để trống");

            if (request.NewPassword.Length < 8)
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 8 ký tự");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[A-Z]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ hoa");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[a-z]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ thường");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[0-9]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ số");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[\W_]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 ký tự đặc biệt");

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return BadRequestResponse("Xác nhận mật khẩu không được để trống");

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequestResponse("Xác nhận mật khẩu không khớp");

            if (request.CurrentPassword == request.NewPassword)
                return BadRequestResponse("Mật khẩu mới phải khác mật khẩu hiện tại");

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
            // Validate request
            if (request == null)
                return BadRequestResponse("Request body không được để trống");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequestResponse("Email không được để trống");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequestResponse("Email không hợp lệ");

            if (request.Email.Length > 255)
                return BadRequestResponse("Email không được vượt quá 255 ký tự");

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
            // Validate request
            if (request == null)
                return BadRequestResponse("Request body không được để trống");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequestResponse("Email không được để trống");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequestResponse("Email không hợp lệ");

            if (string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequestResponse("Mã OTP không được để trống");

            if (request.OtpCode.Length != 6)
                return BadRequestResponse("Mã OTP phải có 6 ký tự");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.OtpCode, @"^\d{6}$"))
                return BadRequestResponse("Mã OTP chỉ được chứa số");

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
            // Validate request
            if (request == null)
                return BadRequestResponse("Request body không được để trống");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequestResponse("Email không được để trống");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequestResponse("Email không hợp lệ");

            if (string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequestResponse("Mã OTP không được để trống");

            if (request.OtpCode.Length != 6)
                return BadRequestResponse("Mã OTP phải có 6 ký tự");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.OtpCode, @"^\d{6}$"))
                return BadRequestResponse("Mã OTP chỉ được chứa số");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequestResponse("Mật khẩu mới không được để trống");

            if (request.NewPassword.Length < 8)
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 8 ký tự");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[A-Z]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ hoa");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[a-z]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ thường");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[0-9]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 chữ số");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, @"[\W_]"))
                return BadRequestResponse("Mật khẩu mới phải có ít nhất 1 ký tự đặc biệt");

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return BadRequestResponse("Xác nhận mật khẩu không được để trống");

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequestResponse("Xác nhận mật khẩu không khớp");

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


    /// <summary>
    /// Login hoặc register bằng Google (KHÔNG HỖ TRỢ CHO WEB)
    /// </summary>
    /// <param name="request">Google login request với ID token</param>
    /// <returns>Login response với JWT tokens</returns>
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var loginResponse = await _userService.GoogleLoginAsync(request);

            if (loginResponse == null)
                return UnauthorizedResponse("Google authentication failed");

            var staffVenueGuardResult = await ValidateStaffVenueAccessAsync(loginResponse.AccessToken);
            if (staffVenueGuardResult != null)
                return staffVenueGuardResult;

            return OkResponse(loginResponse, "Login successful");
        }
        catch (InvalidOperationException ex)
        {
            return ForbiddenResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login error");
            return InternalServerErrorResponse($"Google login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Login hoặc register bằng Google cho mobile app
    /// </summary>
    /// <param name="request">Google login request với ID token</param>
    /// <returns>Login response với JWT tokens</returns>
    [HttpPost("google-login-mobile")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLoginMobile([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var loginResponse = await _userService.GoogleMobileLoginAsync(request);

            if (loginResponse == null)
                return UnauthorizedResponse("Google mobile authentication failed");

            var staffVenueGuardResult = await ValidateStaffVenueAccessAsync(loginResponse.AccessToken);
            if (staffVenueGuardResult != null)
                return staffVenueGuardResult;

            return OkResponse(loginResponse, "Mobile login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google mobile login error");
            return InternalServerErrorResponse($"Google mobile login failed: {ex.Message}");
        }
    }

    private async Task<IActionResult?> ValidateStaffVenueAccessAsync(string accessToken)
    {
        const string blockedMessage = "Venue tạm thời không tồn tại hoặc bị xóa bởi admin, vui lòng liên hệ";

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);

        var role = jwtToken.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
            ?.Value;

        if (!string.Equals(role, "STAFF", StringComparison.OrdinalIgnoreCase))
            return null;

        var email = jwtToken.Claims
            .FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email)
            ?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return UnauthorizedResponse(blockedMessage);

        var currentUser = await _unitOfWork.Users.GetByEmailAsync(email);
        if (currentUser == null || !currentUser.AssignedVenueLocationId.HasValue)
            return UnauthorizedResponse(blockedMessage);

        var assignedVenue = await _unitOfWork.VenueLocations.GetByIdAsync(currentUser.AssignedVenueLocationId.Value);
        if (assignedVenue == null || !string.Equals(assignedVenue.Status, VenueLocationStatus.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase))
            return UnauthorizedResponse(blockedMessage);

        return null;
    }
}
