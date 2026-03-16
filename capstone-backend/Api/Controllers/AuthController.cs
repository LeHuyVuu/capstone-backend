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
}
