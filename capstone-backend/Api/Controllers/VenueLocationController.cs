using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VenueLocationController : BaseController
{
    private readonly IVenueLocationService _venueLocationService;
    private readonly ILogger<VenueLocationController> _logger;

    public VenueLocationController(
        IVenueLocationService venueLocationService,
        ILogger<VenueLocationController> logger)
    {
        _venueLocationService = venueLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get venue location detail by ID.
    /// Returns venue information with location tag (couple mood type and couple personality type) and venue owner profile.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Venue location detail</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVenueLocationById(int id)
    {
        _logger.LogInformation("Requesting venue location detail for ID: {VenueId}", id);

        var venue = await _venueLocationService.GetVenueLocationDetailByIdAsync(id);
        
        if (venue == null)
        {
            return NotFoundResponse($"Venue location with ID {id} not found");
        }

        return OkResponse(venue, "Venue location retrieved successfully");
    }
}
