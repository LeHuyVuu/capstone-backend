using capstone_backend.Api.Models;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace capstone_backend.Api.Filters;

/// <summary>
/// Validates that user has an active subscription package
/// Supports both Member and VenueOwner subscriptions with flexible configuration
/// </summary>
/// <example>
/// Basic usage - auto-detect role:
/// <code>
/// [RequireActiveSubscription]
/// </code>
/// 
/// Force specific user type:
/// <code>
/// [RequireActiveSubscription(UserType = "MEMBER")]
/// [RequireActiveSubscription(UserType = "VENUE_OWNER")]
/// </code>
/// 
/// Custom error message:
/// <code>
/// [RequireActiveSubscription(ErrorMessage = "Subscription expired")]
/// </code>
/// 
/// Custom HTTP status code:
/// <code>
/// [RequireActiveSubscription(ErrorStatusCode = 402)]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireActiveSubscriptionAttribute : ActionFilterAttribute
{
    /// <summary>
    /// User type to validate: "MEMBER", "VENUE_OWNER", or null for auto-detect from role claim
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Optional feature code to validate against package feature flags
    /// Example: "VENUE_INSIGHT"
    /// </summary>
    public string? FeatureCode { get; set; }
    
    /// <summary>
    /// Custom error message
    /// Default: "Gói của bạn đã hết hạn để sài tính năng này"
    /// </summary>
    public string ErrorMessage { get; set; } = "Gói của bạn đã hết hạn để sài tính năng này";
    
    /// <summary>
    /// HTTP status code for error response
    /// Default: 403 (Forbidden)
    /// </summary>
    public int ErrorStatusCode { get; set; } = 403;

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        try
        {
            // Get subscription validation service from DI
            var validationService = context.HttpContext.RequestServices
                .GetService<ISubscriptionValidationService>();
            
            if (validationService == null)
            {
                context.Result = new ObjectResult(ApiResponse<object>.Error(
                    "Subscription validation service not available",
                    500,
                    context.HttpContext.TraceIdentifier))
                {
                    StatusCode = 500
                };
                return;
            }

            // Extract userId from claims
            var userIdClaim = context.HttpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId");
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new ObjectResult(ApiResponse<object>.Error(
                    "User not authenticated",
                    401,
                    context.HttpContext.TraceIdentifier))
                {
                    StatusCode = 401
                };
                return;
            }

            // Determine user type
            string? userType = UserType; // Use property if specified
            
            if (string.IsNullOrEmpty(userType))
            {
                // Auto-detect from role claim
                var roleClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
                
                if (string.IsNullOrEmpty(roleClaim))
                {
                    context.Result = new ObjectResult(ApiResponse<object>.Error(
                        "User role not found",
                        403,
                        context.HttpContext.TraceIdentifier))
                    {
                        StatusCode = 403
                    };
                    return;
                }
                
                userType = roleClaim;
            }

            // Validate subscription
            var (isActive, message) = await validationService.ValidateSubscriptionAsync(userId, userType, FeatureCode);

            if (!isActive)
            {
                // Use custom message if validation returned one, otherwise use ErrorMessage property
                var errorMsg = !string.IsNullOrEmpty(message) ? message : ErrorMessage;
                
                context.Result = new ObjectResult(ApiResponse<object>.Error(
                    errorMsg,
                    ErrorStatusCode,
                    context.HttpContext.TraceIdentifier))
                {
                    StatusCode = ErrorStatusCode
                };
                return;
            }

            // Subscription is active, continue to action
            await next();
        }
        catch (Exception ex)
        {
            // Log exception and return 500
            var logger = context.HttpContext.RequestServices
                .GetService<ILogger<RequireActiveSubscriptionAttribute>>();
            
            logger?.LogError(ex, "Error in RequireActiveSubscriptionAttribute for user");

            context.Result = new ObjectResult(ApiResponse<object>.Error(
                "An error occurred while validating subscription",
                500,
                context.HttpContext.TraceIdentifier))
            {
                StatusCode = 500
            };
        }
    }
}
