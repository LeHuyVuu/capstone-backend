using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "MEMBER, member")]
    public class MemberSubscriptionController : BaseController
    {
        private readonly IMemberSubscriptionService _memberSubscriptionService;

        public MemberSubscriptionController(IMemberSubscriptionService memberSubscriptionService)
        {
            _memberSubscriptionService = memberSubscriptionService;
        }

        /// <summary>
        /// Check payment status (For Members)
        /// </summary>
        [HttpGet("status/{orderId}")]
        public async Task<IActionResult> CheckPaymentStatus([FromRoute] string orderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("Unauthorized");
                }

                var result = await _memberSubscriptionService.CheckPaymentStatusAsync(userId.Value, orderId);
                if (result == null)
                {
                    return NotFoundResponse("Giao dịch không khả dụng");
                }

                return OkResponse(result, "Lấy trạng thái giao dịch thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get current member subscription
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("Unauthorized");
                }
                var result = await _memberSubscriptionService.GetCurrentSubscriptionAsync(userId.Value);
                if (result == null)
                {
                    return NotFoundResponse("Không có gói đăng ký nào đang hoạt động");
                }
                return OkResponse(result, "Lấy thông tin gói đăng ký thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get available subscription packages
        /// </summary>
        [HttpGet("packages")]
        public async Task<IActionResult> GetAvailablePackages([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _memberSubscriptionService.GetAvailablePackagesAsync(pageNumber, pageSize);
                return OkResponse(result, "Lấy danh sách gói đăng ký thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
