using capstone_backend.Business.Interfaces;
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
    }
}
