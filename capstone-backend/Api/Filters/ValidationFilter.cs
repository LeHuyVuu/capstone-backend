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
            // Get first validation error message
            var errorMessage = "Dữ liệu đầu vào không hợp lệ";
            var firstError = context.ModelState.Values
                .FirstOrDefault(x => x?.Errors.Count > 0);
            
            if (firstError?.Errors.Count > 0)
            {
                errorMessage = firstError.Errors.First().ErrorMessage;
            }

            var response = new
            {
                message = errorMessage,
                code = 400,
                data = (object?)null,
                traceId = context.HttpContext.TraceIdentifier,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}
