using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.Interfaces;
using capstone_backend.Extensions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MomoWebhookController : BaseController
    {
        private readonly IMomoService _momoService;
        private readonly ILogger<MomoWebhookController> _logger;

        public MomoWebhookController(IMomoService momoService, ILogger<MomoWebhookController> logger)
        {
            _momoService = momoService;
            _logger = logger;
        }

        [HttpPost("ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleMomoIPN([FromBody] MomoIpnRequest request)
        {

            _logger.LogInformation("Received Momo IPN: {@Request}", JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            try
            {
                // Implement logic
                var isOk = await _momoService.VerifyPaymentProcessing(request);
                if (!isOk)
                    return BadRequest();

                return NoContent();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}
