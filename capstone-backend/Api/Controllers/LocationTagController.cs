using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LocationTagController : BaseController
{
    private readonly IVenueLocationService _venueLocationService;
    private readonly ILogger<LocationTagController> _logger;

    public LocationTagController(
        IVenueLocationService venueLocationService,
        ILogger<LocationTagController> logger)
    {
        _venueLocationService = venueLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all location tags with couple mood type and couple personality type information.
    /// </summary>
    /// <returns>List of all available location tags</returns>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllLocationTags()
    {
        _logger.LogInformation("Requesting all location tags");

        var tags = await _venueLocationService.GetAllLocationTagsAsync();

        return OkResponse(tags, $"Retrieved {tags.Count} location tags");
    }
}
