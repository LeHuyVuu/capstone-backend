using capstone_backend.Business.DTOs.VNPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Emit;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayWebhookController : BaseController
    {
        private readonly ILogger<VNPayWebhookController> _logger;

        public VNPayWebhookController(ILogger<VNPayWebhookController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Callback from VNPay
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleVNPayCallback([FromQuery] VNPayIpnRequest request)
        {
            _logger.LogInformation("Received VNPay IPN: {@Request}", System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));
            // Implement logic to verify and process the payment notification from VNPay
            // You can call your service layer here to handle the business logic
            return Ok(new
            {
                RspCode = "00",
                Message = "IPN received successfully"
            });
        }
    }
}
