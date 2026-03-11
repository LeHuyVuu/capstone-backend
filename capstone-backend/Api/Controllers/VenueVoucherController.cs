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
                return OkResponse(result, "Lấy danh sách voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get voucher details for a venue
        /// </summary>
        [HttpGet("{voucherId:int}")]
        public async Task<IActionResult> GetVenueVoucherDetails(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.GetVoucherByIdAsync(userId.Value, voucherId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy voucher cho địa điểm này");
                return OkResponse(result, "Lấy chi tiết voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get voucher summary for a venue
        /// </summary>
        [HttpGet("{voucherId:int}/summary")]
        public async Task<IActionResult> GetVenueVoucherSummary(int voucherId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User không xác thực");

                var result = await _venueVoucherService.GetVoucherSummaryByIdAsync(userId.Value, voucherId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy voucher cho địa điểm này");
                return OkResponse(result, "Lấy tóm tắt voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Create voucher for a venue
        /// </summary>
        /// <remarks>
        /// DiscountType values:
        /// - Nếu DiscountType = FIXED_AMOUNT thì truyền DiscountAmount
        /// - Nếu DiscountType = PERCENTAGE thì truyền DiscountPercent
        /// - Không truyền đồng thời cả DiscountAmount và DiscountPercent
        /// 
        /// DiscountAmount:
        /// - Số tiền giảm cố định.
        /// - Chỉ dùng khi DiscountType = FIXED_AMOUNT
        /// 
        /// DiscountPercent:
        /// - Phần trăm giảm giá.
        /// - Chỉ dùng khi DiscountType = PERCENTAGE
        /// 
        /// Quantity:
        /// - Tổng số lượng voucher được phát hành
        /// 
        /// UsageLimitPerMember:
        /// - Số lần tối đa một member được sử dụng voucher này
        /// - Để null nếu không giới hạn
        /// 
        /// UsageValidDays:
        /// - Số ngày hiệu lực của mỗi voucher item kể từ lúc member nhận voucher
        /// - FE để default là 7, 14, 20, 30. Còn không thì cho venue owner tự nhập, trong 1-365 ngày
        /// 
        /// VenueLocationIds:
        /// - Danh sách ID địa điểm áp dụng voucher
        /// - Có thể áp dụng cho một hoặc nhiều venue location
        /// </remarks>
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
        /// <remarks>
        /// DiscountType values:
        /// - Nếu DiscountType = FIXED_AMOUNT thì truyền DiscountAmount
        /// - Nếu DiscountType = PERCENTAGE thì truyền DiscountPercent
        /// - Không truyền đồng thời cả DiscountAmount và DiscountPercent
        /// 
        /// DiscountAmount:
        /// - Số tiền giảm cố định.
        /// - Chỉ dùng khi DiscountType = FIXED_AMOUNT
        /// 
        /// DiscountPercent:
        /// - Phần trăm giảm giá.
        /// - Chỉ dùng khi DiscountType = PERCENTAGE
        /// 
        /// Quantity:
        /// - Tổng số lượng voucher được phát hành
        /// 
        /// UsageLimitPerMember:
        /// - Số lần tối đa một member được sử dụng voucher này
        /// - Để null nếu không giới hạn
        /// 
        /// UsageValidDays:
        /// - Số ngày hiệu lực của mỗi voucher item kể từ lúc member nhận voucher
        /// - FE để default là 7, 14, 20, 30. Còn không thì cho venue owner tự nhập, trong 1-365 ngày
        /// 
        /// VenueLocationIds:
        /// - Danh sách ID địa điểm áp dụng voucher
        /// - Có thể áp dụng cho một hoặc nhiều venue location
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
        /// <remarks>Thu hồi yêu cầu duyệt voucher lại</remarks>
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
