using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "MEMBER, member")]
    [ApiController]
    public class MemberAccessoryController : BaseController
    {
        private readonly IAccessoryService _accessoryService;

        public MemberAccessoryController(IAccessoryService accessoryService)
        {
            _accessoryService = accessoryService;
        }

        /// <summary>
        /// Get accessory shop
        /// </summary>
        [HttpGet("shop")]
        public async Task<IActionResult> GetAccessoryShop([FromQuery] GetAccessoryShopRequest query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");

                var result = await _accessoryService.GetShopAsync(userId.Value, query);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
