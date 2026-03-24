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
                return OkResponse(result, "Lấy thông tin thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get accessory detail
        /// </summary>
        [HttpGet("shop/{accessoryId:int}")]
        public async Task<IActionResult> GetAccessoryDetail(int accessoryId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");
                var result = await _accessoryService.GetDetailAsync(userId.Value, accessoryId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy phụ kiện");
                return OkResponse(result, "Lấy thông tin thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Purchase accessory
        /// </summary>
        [HttpPost("shop/{accessoryId:int}/purchase")]
        public async Task<IActionResult> PurchaseAccessory(int accessoryId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");

                var result = await _accessoryService.PurchaseAccessoryAsync(userId.Value, accessoryId);
                if (result == null)
                    return BadRequestResponse("Không thể mua phụ kiện này");

                return OkResponse(result, "Mua phụ kiện thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get inventory of member
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccessories([FromQuery] GetMyAccessoryRequest query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");

                var result = await _accessoryService.GetMyAccessoryAsync(userId.Value, query);

                return OkResponse(result, "Lấy thông tin thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
