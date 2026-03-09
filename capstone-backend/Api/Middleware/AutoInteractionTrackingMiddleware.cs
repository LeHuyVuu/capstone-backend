using capstone_backend.Business.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace capstone_backend.Api.Middleware
{
    /// <summary>
    /// Middleware to automatically track user interactions based on configured routes
    /// No need to add attributes to controllers - just configure routes in InteractionTrackingConfiguration
    /// </summary>
    public class AutoInteractionTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AutoInteractionTrackingMiddleware> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AutoInteractionTrackingMiddleware(
            RequestDelegate next,
            ILogger<AutoInteractionTrackingMiddleware> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("[TRACKING] Request: {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            // Execute the request first
            await _next(context);

            _logger.LogInformation("[TRACKING] Response status: {StatusCode}", 
                context.Response.StatusCode);

            // Only track on successful responses (200-299)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                try
                {
                    // Find matching tracking rule
                    var matchedRule = FindMatchingRule(context);
                    
                    if (matchedRule == null)
                    {
                        _logger.LogInformation("[TRACKING] ❌ No matching rule for {Method} {Path}", 
                            context.Request.Method, context.Request.Path);
                        return;
                    }
                    
                    _logger.LogInformation("[TRACKING] ✅ Matched rule: {InteractionType} on {TargetType}",
                        matchedRule.InteractionType, matchedRule.TargetType);

                    if (matchedRule.Enabled)
                    {
                        // Extract user ID from claims (not member ID!)
                        _logger.LogInformation("[TRACKING] Claims: {Claims}", 
                            string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));
                        
                        var userIdClaim = context.User.Claims
                            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId");

                        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                        {
                            _logger.LogWarning("[TRACKING] ❌ User not authenticated or invalid userId claim");
                            return;
                        }
                        
                        _logger.LogInformation("[TRACKING] ✅ Extracted userId: {UserId}", userId);

                        // Extract couple ID if available
                        var coupleIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "coupleId");
                        int? coupleId = null;
                        if (coupleIdClaim != null && int.TryParse(coupleIdClaim.Value, out int parsedCoupleId))
                        {
                            coupleId = parsedCoupleId;
                        }

                        // Extract target ID from route
                        var targetId = ExtractTargetId(context, matchedRule);
                        
                        if (!targetId.HasValue)
                        {
                            _logger.LogWarning("[TRACKING] ❌ Could not extract targetId from {Path}", 
                                context.Request.Path);
                            return;
                        }
                        
                        _logger.LogInformation("[TRACKING] ✅ Extracted targetId: {TargetId}", targetId.Value);

                        // Capture values for async task
                        var interactionType = matchedRule.InteractionType;
                        var targetType = matchedRule.TargetType;
                        var targetIdValue = targetId.Value;
                        var requestMethod = context.Request.Method;
                        var requestPath = context.Request.Path.Value;

                        // Track interaction asynchronously in a new scope (fire and forget)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Create a new scope to get fresh DbContext
                                using var scope = _serviceScopeFactory.CreateScope();
                                var dbContext = scope.ServiceProvider.GetRequiredService<capstone_backend.Data.Context.MyDbContext>();
                                var trackingService = scope.ServiceProvider.GetRequiredService<IInteractionTrackingService>();

                                _logger.LogInformation("[TRACKING] 🔄 Created new scope for async tracking");

                                // Query MemberProfile in new scope
                                var memberProfile = await dbContext.MemberProfiles
                                    .FirstOrDefaultAsync(m => m.UserId == userId);

                                if (memberProfile == null)
                                {
                                    _logger.LogWarning("[TRACKING] ❌ No MemberProfile found for userId: {UserId} in async task", userId);
                                    return;
                                }

                                int memberId = memberProfile.Id;
                                _logger.LogInformation("[TRACKING] ✅ Found memberId: {MemberId} for userId: {UserId} in async task", 
                                    memberId, userId);

                                // Extract category from target entity
                                string? categoryInteraction = await ExtractCategoryAsync(dbContext, targetType, targetIdValue);
                                _logger.LogInformation("[TRACKING] 📂 Extracted category: {Category}", 
                                    categoryInteraction ?? "(null)");

                                _logger.LogInformation("[TRACKING] 🚀 Starting track: Member {MemberId} {InteractionType} {TargetType} {TargetId} Category {Category}",
                                    memberId, interactionType, targetType, targetIdValue, categoryInteraction ?? "(null)");

                                await trackingService.TrackInteractionAsync(
                                    memberId,
                                    coupleId,
                                    interactionType,
                                    targetType,
                                    targetIdValue,
                                    categoryInteraction);

                                _logger.LogInformation(
                                    "[TRACKING] ✅ SUCCESS: {Method} {Path} -> {InteractionType} on {TargetType} {TargetId} Category {Category} by Member {MemberId}",
                                    requestMethod,
                                    requestPath,
                                    interactionType,
                                    targetType,
                                    targetIdValue,
                                    categoryInteraction ?? "(null)",
                                    memberId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[TRACKING] ❌ FAILED to auto-track interaction for {Path}", requestPath);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Don't let tracking failures break the response
                    _logger.LogError(ex, "Error in auto-tracking middleware for {Path}", context.Request.Path);
                }
            }
        }

        /// <summary>
        /// Find a tracking rule that matches the current request
        /// </summary>
        private InteractionTrackingRule? FindMatchingRule(HttpContext context)
        {
            var requestPath = context.Request.Path.Value?.ToLower() ?? string.Empty;
            var requestMethod = context.Request.Method.ToUpper();

            foreach (var rule in InteractionTrackingConfiguration.TrackingRules)
            {
                // Check method match
                if (rule.Method != "*" && !rule.Method.Equals(requestMethod, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check route pattern match
                if (IsRouteMatch(requestPath, rule.RoutePattern))
                {
                    return rule;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if request path matches route pattern
        /// Supports route parameters like {id}, {venueId}, etc.
        /// </summary>
        private bool IsRouteMatch(string requestPath, string routePattern)
        {
            // Convert route pattern to regex
            // Example: /api/VenueLocation/{id} -> ^/api/venuelocation/(\d+)$
            var pattern = routePattern.ToLower()
                .Replace("{id}", @"(\d+)")
                .Replace("{venueid}", @"(\d+)")
                .Replace("{challengeid}", @"(\d+)")
                .Replace("{advertisementid}", @"(\d+)")
                .Replace("{postid}", @"(\d+)")
                .Replace("{planid}", @"(\d+)");

            // Handle any other route parameters
            pattern = Regex.Replace(pattern, @"\{[a-zA-Z0-9_]+\}", @"(\d+)");

            pattern = "^" + pattern + "$";

            return Regex.IsMatch(requestPath, pattern);
        }

        /// <summary>
        /// Extract target ID from route values or query string
        /// </summary>
        private int? ExtractTargetId(HttpContext context, InteractionTrackingRule rule)
        {
            // Try to get from route values first
            if (context.Request.RouteValues.ContainsKey(rule.TargetIdParameter))
            {
                var routeValue = context.Request.RouteValues[rule.TargetIdParameter];
                if (routeValue != null && int.TryParse(routeValue.ToString(), out int routeId))
                {
                    return routeId;
                }
            }

            // Try from query string
            if (context.Request.Query.ContainsKey(rule.TargetIdParameter))
            {
                if (int.TryParse(context.Request.Query[rule.TargetIdParameter], out int queryId))
                {
                    return queryId;
                }
            }

            // Try to extract from path using regex
            var requestPath = context.Request.Path.Value ?? string.Empty;
            var pattern = rule.RoutePattern
                .Replace("{id}", @"(?<id>\d+)")
                .Replace("{venueId}", @"(?<venueId>\d+)")
                .Replace("{challengeId}", @"(?<challengeId>\d+)")
                .Replace("{advertisementId}", @"(?<advertisementId>\d+)")
                .Replace("{postId}", @"(?<postId>\d+)")
                .Replace("{planId}", @"(?<planId>\d+)");

            // Handle any other named parameters
            pattern = Regex.Replace(pattern, @"\{([a-zA-Z0-9_]+)\}", @"(?<$1>\d+)");

            var match = Regex.Match(requestPath, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups[rule.TargetIdParameter].Success)
            {
                if (int.TryParse(match.Groups[rule.TargetIdParameter].Value, out int pathId))
                {
                    return pathId;
                }
            }

            return null;
        }

        /// <summary>
        /// Extract category/theme from target entity for context-aware recommendations
        /// VenueLocation: get Category field directly
        /// Advertisement: get Category from linked VenueLocation
        /// Challenge/Collection: return null (or add logic later)
        /// </summary>
        private async Task<string?> ExtractCategoryAsync(
            capstone_backend.Data.Context.MyDbContext dbContext, 
            string targetType, 
            int targetId)
        {
            try
            {
                switch (targetType)
                {
                    case "VenueLocation":
                        var venue = await dbContext.VenueLocations
                            .Where(v => v.Id == targetId)
                            .Select(v => v.Category)
                            .FirstOrDefaultAsync();
                        return venue;

                    case "Advertisement":
                        var ad = await dbContext.Advertisements
                            .Where(a => a.Id == targetId)
                            .Select(a => a.Category)
                            .FirstOrDefaultAsync();
                        return ad;

                    case "Challenge":
                        // TODO: Add challenge category logic if needed
                        return null;

                    case "Collection":
                        // TODO: Add collection category logic if needed
                        return null;

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TRACKING] Failed to extract category for {TargetType} {TargetId}", 
                    targetType, targetId);
                return null;
            }
        }
    }
}
