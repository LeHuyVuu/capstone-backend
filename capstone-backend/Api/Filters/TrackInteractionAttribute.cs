using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace capstone_backend.Api.Filters
{
    /// <summary>
    /// Attribute to automatically track user interactions with entities.
    /// Usage: [TrackInteraction("VIEW", "VenueLocation", "id")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TrackInteractionAttribute : ActionFilterAttribute
    {
        private readonly string _interactionType;
        private readonly string _targetType;
        private readonly string _targetIdParameter;

        /// <summary>
        /// Create interaction tracking attribute
        /// </summary>
        /// <param name="interactionType">Type: VIEW, CLICK, FAVORITE, SHARE</param>
        /// <param name="targetType">Entity type: VenueLocation, Advertisement, CoupleMoodType, etc.</param>
        /// <param name="targetIdParameter">Name of the route/query parameter containing target ID (default: "id")</param>
        public TrackInteractionAttribute(
            string interactionType, 
            string targetType, 
            string targetIdParameter = "id")
        {
            _interactionType = interactionType;
            _targetType = targetType;
            _targetIdParameter = targetIdParameter;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context, 
            ActionExecutionDelegate next)
        {
            // Execute the action first
            var executedContext = await next();

            // Only track on successful responses
            if (executedContext.Result is OkObjectResult || executedContext.Result is ObjectResult)
            {
                try
                {
                    // Get tracking service from DI
                    var trackingService = context.HttpContext.RequestServices
                        .GetService<IInteractionTrackingService>();

                    if (trackingService == null)
                    {
                        return; // Service not registered
                    }

                    // Extract user ID from claims (not member ID!)
                    var userIdClaim = context.HttpContext.User.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId");

                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return; // User not authenticated or invalid user ID
                    }

                    // Get MemberProfile to retrieve memberId from userId
                    var dbContext = context.HttpContext.RequestServices
                        .GetService<capstone_backend.Data.Context.MyDbContext>();
                    
                    if (dbContext == null)
                    {
                        return; // DbContext not available
                    }

                    var memberProfile = await dbContext.MemberProfiles
                        .FirstOrDefaultAsync(m => m.UserId == userId);

                    if (memberProfile == null)
                    {
                        return; // User doesn't have a member profile yet
                    }

                    int memberId = memberProfile.Id;

                    // Extract couple ID if available
                    var coupleIdClaim = context.HttpContext.User.Claims
                        .FirstOrDefault(c => c.Type == "coupleId");
                    int? coupleId = null;
                    if (coupleIdClaim != null && int.TryParse(coupleIdClaim.Value, out int parsedCoupleId))
                    {
                        coupleId = parsedCoupleId;
                    }

                    // Extract target ID from route parameters or query string
                    int? targetId = null;

                    // Try route values first
                    if (context.ActionArguments.ContainsKey(_targetIdParameter))
                    {
                        var arg = context.ActionArguments[_targetIdParameter];
                        if (arg is int intValue)
                        {
                            targetId = intValue;
                        }
                        else if (int.TryParse(arg?.ToString(), out int parsedValue))
                        {
                            targetId = parsedValue;
                        }
                    }

                    // Try query string if not in route
                    if (!targetId.HasValue && context.HttpContext.Request.Query.ContainsKey(_targetIdParameter))
                    {
                        if (int.TryParse(context.HttpContext.Request.Query[_targetIdParameter], out int queryValue))
                        {
                            targetId = queryValue;
                        }
                    }

                    if (targetId.HasValue)
                    {
                        // Track interaction asynchronously (fire and forget)
                        _ = trackingService.TrackInteractionAsync(
                            memberId,
                            coupleId,
                            _interactionType,
                            _targetType,
                            targetId.Value);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
