using capstone_backend.Api.Models;
using capstone_backend.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Filters;

/// <summary>
/// Action filter attribute để kiểm tra subscription còn hạn sử dụng hay không
/// Áp dụng cho Member và VenueOwner roles
/// </summary>
/// <example>
/// [RequireActiveSubscription]
/// [RequireActiveSubscription(GracePeriodDays = 7)]
/// [RequireActiveSubscription(RequiredPackageType = "PREMIUM", CustomErrorMessage = "Premium subscription required")]
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireActiveSubscriptionAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// Custom error message khi validation fails
    /// </summary>
    public string? CustomErrorMessage { get; set; }

    /// <summary>
    /// Grace period (số ngày) sau khi subscription hết hạn vẫn cho phép truy cập
    /// Default = 0 (không có grace period)
    /// </summary>
    public int GracePeriodDays { get; set; } = 0;

    /// <summary>
    /// Yêu cầu package type cụ thể (MEMBER hoặc VENUE)
    /// Null = chấp nhận bất kỳ package type nào
    /// </summary>
    public string? RequiredPackageType { get; set; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<RequireActiveSubscriptionAttribute>>();
        var dbContext = httpContext.RequestServices.GetRequiredService<MyDbContext>();
        
        var traceId = httpContext.Items["TraceId"]?.ToString() ?? httpContext.TraceIdentifier;
        var endpoint = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        try
        {
            // 1. Check authentication
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                logger.LogWarning("Subscription check failed: User not authenticated. Endpoint={Endpoint}, TraceId={TraceId}", 
                    endpoint, traceId);
                context.Result = CreateErrorResponse("Authentication required", 401, traceId);
                return;
            }

            // 2. Extract UserId from JWT claims
            var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                             ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("Subscription check failed: Invalid user identifier. Endpoint={Endpoint}, TraceId={TraceId}", 
                    endpoint, traceId);
                context.Result = CreateErrorResponse("Invalid user identifier", 403, traceId);
                return;
            }

            // 3. Extract Role from JWT claims
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(role))
            {
                logger.LogWarning("Subscription check failed: Invalid user role. UserId={UserId}, Endpoint={Endpoint}, TraceId={TraceId}", 
                    userId, endpoint, traceId);
                context.Result = CreateErrorResponse("Invalid user role", 403, traceId);
                return;
            }

            logger.LogInformation("Subscription validation started: UserId={UserId}, Role={Role}, Endpoint={Endpoint}, TraceId={TraceId}", 
                userId, role, endpoint, traceId);

            // 4. Validate subscription based on role
            var currentTime = DateTime.UtcNow;
            bool hasValidSubscription;
            string errorMessage;

            if (string.Equals(role, "Member", StringComparison.OrdinalIgnoreCase))
            {
                (hasValidSubscription, errorMessage) = await ValidateMemberSubscriptionAsync(
                    dbContext, userId, currentTime, logger);
            }
            else if (string.Equals(role, "VenueOwner", StringComparison.OrdinalIgnoreCase))
            {
                (hasValidSubscription, errorMessage) = await ValidateVenueOwnerSubscriptionAsync(
                    dbContext, userId, currentTime, logger);
            }
            else
            {
                logger.LogWarning("Subscription check not applicable: UserId={UserId}, Role={Role}, Endpoint={Endpoint}, TraceId={TraceId}", 
                    userId, role, endpoint, traceId);
                context.Result = CreateErrorResponse("Subscription check not applicable for this role", 403, traceId);
                return;
            }

            // 5. Return result
            if (!hasValidSubscription)
            {
                var finalErrorMessage = CustomErrorMessage ?? errorMessage;
                logger.LogWarning("Subscription validation failed: UserId={UserId}, Role={Role}, Reason={Reason}, Endpoint={Endpoint}, TraceId={TraceId}", 
                    userId, role, finalErrorMessage, endpoint, traceId);
                context.Result = CreateErrorResponse(finalErrorMessage, 403, traceId);
                return;
            }

            logger.LogInformation("Subscription validation succeeded: UserId={UserId}, Role={Role}, Endpoint={Endpoint}, TraceId={TraceId}", 
                userId, role, endpoint, traceId);

            // Continue to controller action
            await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Subscription validation error: Endpoint={Endpoint}, TraceId={TraceId}", endpoint, traceId);
            context.Result = CreateErrorResponse("Failed to verify subscription", 500, traceId);
        }
    }

    /// <summary>
    /// Validate Member subscription
    /// </summary>
    private async Task<(bool isValid, string errorMessage)> ValidateMemberSubscriptionAsync(
        MyDbContext dbContext, 
        int userId, 
        DateTime currentTime,
        ILogger logger)
    {
        // 1. Get MemberProfile
        var memberProfile = await dbContext.MemberProfiles
            .Where(m => m.UserId == userId && m.IsDeleted != true)
            .Select(m => new { m.Id, m.IsDeleted })
            .FirstOrDefaultAsync();

        if (memberProfile == null)
        {
            return (false, "Member profile not found");
        }

        if (memberProfile.IsDeleted == true)
        {
            return (false, "Member profile has been deleted");
        }

        // 2. Check for active subscriptions
        var effectiveEndDate = currentTime.AddDays(-GracePeriodDays);
        
        var query = dbContext.MemberSubscriptionPackages
            .Where(s => s.MemberId == memberProfile.Id
                     && s.Status != null
                     && s.StartDate != null
                     && s.EndDate != null
                     && s.StartDate <= currentTime
                     && s.EndDate >= effectiveEndDate);

        // Apply package type filter if specified
        if (!string.IsNullOrEmpty(RequiredPackageType))
        {
            query = query.Where(s => s.Package != null && s.Package.Type == RequiredPackageType);
        }

        var hasValidSubscription = await query
            .AnyAsync(s => s.Status.ToLower() == "active");

        if (hasValidSubscription)
        {
            // Log subscription details
            var subscription = await query
                .Where(s => s.Status.ToLower() == "active")
                .Select(s => new { s.Id, s.Status, s.StartDate, s.EndDate })
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                logger.LogInformation("Found valid subscription: SubscriptionId={SubscriptionId}, Status={Status}, StartDate={StartDate}, EndDate={EndDate}", 
                    subscription.Id, subscription.Status, subscription.StartDate, subscription.EndDate);
            }

            return (true, string.Empty);
        }

        return (false, "No valid subscription found");
    }

    /// <summary>
    /// Validate VenueOwner subscription
    /// </summary>
    private async Task<(bool isValid, string errorMessage)> ValidateVenueOwnerSubscriptionAsync(
        MyDbContext dbContext, 
        int userId, 
        DateTime currentTime,
        ILogger logger)
    {
        // 1. Get VenueOwnerProfile
        var venueOwnerProfile = await dbContext.VenueOwnerProfiles
            .Where(v => v.UserId == userId && v.IsDeleted != true)
            .Select(v => new { v.Id, v.IsDeleted })
            .FirstOrDefaultAsync();

        if (venueOwnerProfile == null)
        {
            return (false, "Venue owner profile not found");
        }

        if (venueOwnerProfile.IsDeleted == true)
        {
            return (false, "Venue owner profile has been deleted");
        }

        // 2. Get all venue IDs for this owner
        var venueIds = await dbContext.VenueLocations
            .Where(v => v.VenueOwnerId == venueOwnerProfile.Id && v.IsDeleted != true)
            .Select(v => v.Id)
            .ToListAsync();

        if (!venueIds.Any())
        {
            return (false, "No venue found for this owner");
        }

        // 3. Check for active subscriptions across all venues
        var effectiveEndDate = currentTime.AddDays(-GracePeriodDays);
        
        var query = dbContext.VenueSubscriptionPackages
            .Where(s => venueIds.Contains(s.VenueId)
                     && s.Status != null
                     && s.StartDate != null
                     && s.EndDate != null
                     && s.StartDate <= currentTime
                     && s.EndDate >= effectiveEndDate);

        // Apply package type filter if specified
        if (!string.IsNullOrEmpty(RequiredPackageType))
        {
            query = query.Where(s => s.Package != null && s.Package.Type == RequiredPackageType);
        }

        var hasValidSubscription = await query
            .AnyAsync(s => s.Status.ToLower() == "active");

        if (hasValidSubscription)
        {
            // Log subscription details
            var subscription = await query
                .Where(s => s.Status.ToLower() == "active")
                .Select(s => new { s.Id, s.VenueId, s.Status, s.StartDate, s.EndDate })
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                logger.LogInformation("Found valid subscription: SubscriptionId={SubscriptionId}, VenueId={VenueId}, Status={Status}, StartDate={StartDate}, EndDate={EndDate}", 
                    subscription.Id, subscription.VenueId, subscription.Status, subscription.StartDate, subscription.EndDate);
            }

            return (true, string.Empty);
        }

        return (false, "No valid subscription found for any venue");
    }

    /// <summary>
    /// Create standardized error response in ApiResponse format
    /// </summary>
    private ObjectResult CreateErrorResponse(string message, int statusCode, string traceId)
    {
        var response = new
        {
            message,
            code = statusCode,
            data = (object?)null,
            traceId,
            timestamp = DateTime.UtcNow
        };

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }
}
