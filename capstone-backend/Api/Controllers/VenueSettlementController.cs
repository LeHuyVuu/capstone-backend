using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "VENUEOWNER, venueowner")]
    [ApiController]
    public class VenueSettlementController : BaseController
    {
        private readonly IVenueSettlementService _venueSettlementService;

        public VenueSettlementController(IVenueSettlementService venueSettlementService)
        {
            _venueSettlementService = venueSettlementService;
        }

        /// <summary>
        /// Get summary settlement of vouchers
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummarySettlement()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();

                var result = await _venueSettlementService.GetSummaryAsync(userId.Value);
                return OkResponse(result, "Lấy tổng quan đối soát thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
