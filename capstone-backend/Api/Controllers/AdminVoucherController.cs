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
    }
}
