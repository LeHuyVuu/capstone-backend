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
        /// Get list of vouchers for a venue
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVenueVouchers([FromQuery] GetVenueVouchersRequest query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.GetVenueVouchersAsync(userId.Value, query);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
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

        /// <summary>
        /// Update voucher for a venue
        /// </summary>
        [HttpPut("{voucherId:int}")]
        public async Task<IActionResult> UpdateVenueVoucher(int voucherId, [FromBody] UpdateVoucherRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.UpdateVenueVoucherAsync(userId.Value, voucherId, request);
                if (result == null)
                    return BadRequestResponse("Không thể cập nhật voucher cho địa điểm này");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete voucher for a venue
        /// </summary>
        [HttpDelete("{voucherId:int}")]
        public async Task<IActionResult> DeleteVenueVoucher(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var success = await _venueVoucherService.DeleteVenueVoucherAsync(userId.Value, voucherId);
                if (!success)
                    return BadRequestResponse("Không thể xóa voucher cho địa điểm này");
                return OkResponse("Xóa voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Submit voucher for approval
        /// </summary>
        [HttpPost("{voucherId:int}/submit")]
        public async Task<IActionResult> SubmitVenueVoucher(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.SubmitVoucherAsync(userId.Value, voucherId);
                if (result == null)
                    return BadRequestResponse("Không thể gửi voucher để xét duyệt");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Revoke submitted voucher
        /// </summary>
        [HttpPost("{voucherId:int}/revoke")]
        public async Task<IActionResult> RevokeVenueVoucher(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.RevokeSubmittedVoucherAsync(userId.Value, voucherId);
                if (result == null)
                    return BadRequestResponse("Không thể thu hồi yêu cầu duyệt voucher");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
