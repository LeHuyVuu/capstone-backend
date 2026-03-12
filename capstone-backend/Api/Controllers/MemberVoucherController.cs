using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/member-vouchers")]
    [Authorize(Roles = "MEMBER, member")]
    [ApiController]
    public class MemberVoucherController : BaseController
    {
        private readonly IMemberVoucherService _memberVoucherService;

        public MemberVoucherController(IMemberVoucherService memberVoucherService)
        {
            _memberVoucherService = memberVoucherService;
        }

        /// <summary>
        /// Get list of vouchers for member
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMemberVouchers([FromQuery] GetMemberVouchersRequest request)
        {
            try
            {
                var result = await _memberVoucherService.GetMemberVouchersAsync(request);
                return OkResponse(result, "Lấy danh sách voucher thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
