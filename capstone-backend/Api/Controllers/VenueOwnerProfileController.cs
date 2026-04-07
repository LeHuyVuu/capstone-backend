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
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        try
        {
            var result = await _venueOwnerProfileService.UpdateVenueOwnerProfileAsync(userId.Value, request);
            if (result == null)
                return NotFoundResponse("Không tìm thấy hồ sơ chủ địa điểm");

            return OkResponse(result, "Cập nhật hồ sơ chủ địa điểm thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResponse(ex.Message);
        }
    }
}
