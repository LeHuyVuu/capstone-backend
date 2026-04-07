using capstone_backend.Business.DTOs.SystemConfig;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN, admin")]
    [ApiController]
    public class SystemConfigController : BaseController
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        /// <summary>
        /// Get a system configuration by key
        /// </summary>
        [HttpGet("keys")]
        public async Task<IActionResult> GetByKey([FromQuery] SystemConfigKeys key)
        {
            if (!Enum.IsDefined(typeof(SystemConfigKeys), key))
            {
                return BadRequestResponse("Khóa cấu hình không hợp lệ");
            }

            var result = await _systemConfigService.GetByKeyAsync(key.ToString());
            return OkResponse(result);
        }

        /// <summary>
        /// Get all system configurations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllConfigs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var configs = await _systemConfigService.GetAllConfigsAsync(pageNumber, pageSize);
                return OkResponse(configs, "Lấy cấu hình hệ thống thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update a system configuration by key
        /// </summary>
        /// <remarks>
        /// Key:
        /// 
        ///     - MONEY_TO_POINT_RATE
        /// 
        ///     - VENUE_COMMISSION_PERCENT
        /// 
        /// Value: truyền string
        /// </remarks>
        [HttpPut]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigRequest request)
        {
            try
            {
                var result = await _systemConfigService.UpdateConfigAsync(request);
                return OkResponse(result, "Cập nhật cấu hình thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
