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
        [HttpGet("status/{transactionId:int}")]
        public async Task<IActionResult> CheckPaymentStatus(int transactionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("Unauthorized");
                }

                var result = await _memberSubscriptionService.CheckPaymentStatusAsync(userId.Value, transactionId);
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
    }
}
