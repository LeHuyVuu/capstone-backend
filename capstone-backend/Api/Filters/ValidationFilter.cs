using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace capstone_backend.Api.Filters;

/// <summary>
/// Action filter to automatically handle ModelState validation errors
/// </summary>
/// <remarks>
/// Intercepts requests with invalid ModelState (from DataAnnotations or FluentValidation)
/// and returns a standardized error response in ApiResponse format.
/// This runs after both DataAnnotations and FluentValidation.
/// </remarks>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var traceId = context.HttpContext.Items["TraceId"]?.ToString()
                          ?? context.HttpContext.TraceIdentifier;

            // Sử dụng ApiResponse.ErrorData để trả về validation errors
            var response = ApiResponse<object>.ErrorData(
                errors, 
                "Validation failed", 
                400, 
                traceId
            );

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}
