using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/venue-vouchers")]
    [ApiController]
    [Authorize(Roles = "VENUEOWNER, venueowner")]
    public class VenueVoucherController : BaseController
    {
        private readonly IVenueVoucherService _venueVoucherService;

        public VenueVoucherController(IVenueVoucherService venueVoucherService)
        {
            _venueVoucherService = venueVoucherService;
        }

        /// <summary>
        /// Create voucher for a venue
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateVenueVoucher([FromBody] CreateVoucherRequest request)
        {
            try
            {

                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.CreateVenueVoucherAsync(userId.Value, request);
                if (result == null)
                    return BadRequestResponse("Không thể tạo voucher cho địa điểm này");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
