using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VNPAY;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayController : BaseController
    {
        private readonly IVnpayClient _vnpayClient;
        private readonly ILogger<VNPayController> _logger;

        public VNPayController(IVnpayClient vnpayClient, ILogger<VNPayController> logger)
        {
            _vnpayClient = vnpayClient;
            _logger = logger;
        }

        [HttpPost("ipn")]
        public IActionResult Ipn()
        {
            var paymentResult = _vnpayClient.GetPaymentResult(this.Request);
            var jsonString = System.Text.Json.JsonSerializer.Serialize(paymentResult);
            _logger.LogInformation("VNPay IPN received: {PaymentResult}", jsonString);

            return Ok(jsonString);
        }
    }
}
