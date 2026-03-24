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

        /// <summary>
        /// Get inventory detail of member
        /// </summary>
        [HttpGet("me/{memberAccessoryId:int}")]
        public async Task<IActionResult> GetMyAccessoryDetail(int memberAccessoryId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");
                var result = await _accessoryService.GetMyAccessoryDetailAsync(userId.Value, memberAccessoryId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy phụ kiện trong kho của bạn");
                return OkResponse(result, "Lấy thông tin thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Equip accessory for member
        /// </summary>
        [HttpPost("me/{memberAccessoryId:int}/equip")]
        public async Task<IActionResult> EquipAccessory(int memberAccessoryId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");

                var result = await _accessoryService.EquipAccessoryAsync(userId.Value, memberAccessoryId);
                if (result == null)
                    return BadRequestResponse("Không thể trang bị phụ kiện này");

                return OkResponse(result, "Trang bị phụ kiện thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Unequip accessory for member
        /// </summary>
        [HttpPost("me/{memberAccessoryId:int}/unequip")]
        public async Task<IActionResult> UnequipAccessory(int memberAccessoryId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse("User not authenticated");
                var result = await _accessoryService.UnequipAccessoryAsync(userId.Value, memberAccessoryId);

                if (result == null)
                    return BadRequestResponse("Không thể tháo phụ kiện này");
                return OkResponse(result, "Tháo phụ kiện thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
