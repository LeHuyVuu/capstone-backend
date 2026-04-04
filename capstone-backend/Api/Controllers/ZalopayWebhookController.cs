using capstone_backend.Business.DTOs.Zalo;
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

        public ZalopayWebhookController(ILogger<ZalopayWebhookController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Callback from ZaloPay
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> HandleZaloPayCallback([FromBody] ZaloPayCallbackRequest request)
        {
            var rawPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });

            _logger.LogInformation("=== DATA ZALOPAY NÉM VỀ ===\n{Payload}\n===========================", rawPayload);

            return Ok(new { return_code = 1, return_message = "success" });
        }
    }
}
