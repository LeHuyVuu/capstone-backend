using capstone_backend.Business.DTOs.Zalo;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZalopayWebhookController : BaseController
    {
        private readonly ILogger<ZalopayWebhookController> _logger;
        private readonly IZaloPayService _zaloPayService;

        public ZalopayWebhookController(ILogger<ZalopayWebhookController> logger, IZaloPayService zaloPayService)
        {
            _logger = logger;
            _zaloPayService = zaloPayService;
        }

        /// <summary>
        /// Callback from ZaloPay
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleZaloPayCallback([FromBody] ZaloPayCallbackRequest request)
        {
            _logger.LogInformation("Received ZALO IPN: {@Request}", JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            // Implement logic
            var isOk = await _zaloPayService.VerifyPaymentProcessing(request);
            if (!isOk)
                return BadRequest(new { return_code = 2, return_message = "MAC không hợp lệ" });

            return Ok(new { return_code = 1, return_message = "success" });
        }
    }
}
