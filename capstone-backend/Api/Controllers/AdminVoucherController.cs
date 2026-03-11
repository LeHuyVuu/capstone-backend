using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/admin-vouchers")]
    [ApiController]
    [Authorize(Roles = "ADMIN, admin")]
    public class AdminVoucherController : BaseController
    {
        private readonly IAdminVoucherService _adminVoucherService;

        public AdminVoucherController(IAdminVoucherService adminVoucherService)
        {
            _adminVoucherService = adminVoucherService;
        }

        /// <summary>
        /// Get list of vouchers for admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAdminVouchers([FromQuery] GetAdminVouchersRequest query)
        {
            try
            {
                var result = await _adminVoucherService.GetAdminVouchersAsync(query);
                return OkResponse(result, "Lấy danh sách voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get voucher details for admin
        /// </summary>
        [HttpGet("{voucherId:int}")]
        public async Task<IActionResult> GetAdminVoucherDetails(int voucherId)
        {
            try
            {
                var result = await _adminVoucherService.GetAdminVoucherByIdAsync(voucherId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy voucher");
                return OkResponse(result, "Lấy chi tiết voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get pending vouchers for admin
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingVouchers([FromQuery] GetPendingVouchersRequest query)
        {
            try
            {
                var result = await _adminVoucherService.GetPendingVouchersAsync(query);
                return OkResponse(result, "Lấy danh sách voucher đang chờ duyệt thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Approve a pending voucher
        /// </summary>
        [HttpPost("{voucherId:int}/approve")]
        public async Task<IActionResult> ApproveVoucher(int voucherId)
        {
            try
            {
                var result = await _adminVoucherService.ApproveVoucherAsync(voucherId);

                if (result <= 0)
                    return NotFoundResponse("Duyệt voucher không thành công");

                return OkResponse(result, "Duyệt voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Reject a pending voucher
        /// </summary>
        [HttpPost("{voucherId:int}/reject")]
        public async Task<IActionResult> RejectVoucher(int voucherId, [FromBody] RejectReasonRequest request)
        {
            try
            {
                var result = await _adminVoucherService.RejectVoucherAsync(voucherId, request);
                if (result <= 0)
                    return NotFoundResponse("Từ chối voucher không thành công");
                return OkResponse(result, "Từ chối voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
