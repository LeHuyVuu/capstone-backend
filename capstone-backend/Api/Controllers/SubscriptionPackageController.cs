using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubscriptionPackageController : BaseController
{
    private readonly ISubscriptionPackageService _subscriptionPackageService;
    private readonly ILogger<SubscriptionPackageController> _logger;

    public SubscriptionPackageController(
        ISubscriptionPackageService subscriptionPackageService,
        ILogger<SubscriptionPackageController> logger)
    {
        _subscriptionPackageService = subscriptionPackageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all subscription packages by type (MEMBER or VENUE)
    /// </summary>
    /// <param name="type">Package type: MEMBER or VENUE</param>
    /// <returns>List of subscription packages</returns>
    /// <response code="200">Returns the list of subscription packages</response>
    /// <response code="400">If the type is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubscriptionPackagesByType([FromQuery] string type)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequestResponse("Type parameter is required");
            }

            var packages = await _subscriptionPackageService.GetSubscriptionPackagesByTypeAsync(type);
            
            return OkResponse(packages, $"Successfully retrieved {packages.Count} subscription packages");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when getting subscription packages");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription packages by type: {Type}", type);
            return InternalServerErrorResponse("An error occurred while retrieving subscription packages");
        }
    }
}
