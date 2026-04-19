using System.Net;
using System.Security.Claims;
using System.Text.Json;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Api.Middleware;

public class ActiveUserGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ActiveUserGuardMiddleware> _logger;

    public ActiveUserGuardMiddleware(
        RequestDelegate next,
        ILogger<ActiveUserGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? user.FindFirstValue("userId");

            if (int.TryParse(userIdClaim, out var userId))
            {
                var account = await unitOfWork.Users.GetByIdAsync(userId);

                if (account == null || account.IsActive != true)
                {
                    _logger.LogWarning("Blocked request for inactive or missing user account {UserId}", userId);
                    await WriteLockedResponseAsync(context);
                    return;
                }
            }
        }

        await _next(context);
    }

    private static async Task WriteLockedResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "Tài khoản đã bị khóa",
            code = (int)HttpStatusCode.Unauthorized,
            data = (object?)null,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
