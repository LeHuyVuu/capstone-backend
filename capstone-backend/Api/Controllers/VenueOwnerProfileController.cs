using capstone_backend.Business.DTOs.VenueOwner;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/venue-owner-profile")]
[Authorize(Roles = "VENUEOWNER")]
public class VenueOwnerProfileController : BaseController
{
    private readonly IVenueOwnerProfileService _venueOwnerProfileService;

    public VenueOwnerProfileController(IVenueOwnerProfileService venueOwnerProfileService)
    {
        _venueOwnerProfileService = venueOwnerProfileService;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateVenueOwnerProfile([FromBody] UpdateVenueOwnerProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse("User not authenticated");

        try
        {
            var result = await _venueOwnerProfileService.UpdateVenueOwnerProfileAsync(userId.Value, request);
            if (result == null)
                return NotFoundResponse("Venue owner profile not found");

            return OkResponse(result, "Venue owner profile updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResponse(ex.Message);
        }
    }
}
