using capstone_backend.Business.DTOs.VNPay;
using capstone_backend.Business.Interfaces;
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
        private readonly IVNPayService _vnPayService;

        public VNPayWebhookController(ILogger<VNPayWebhookController> logger, IVNPayService vnPayService)
        {
            _logger = logger;
            _vnPayService = vnPayService;
        }

        /// <summary>
        /// Callback from VNPay
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleVNPayCallback()
        {
            try
            {
                _logger.LogInformation("[VNPAY IPN] Raw QueryString: {QueryString}", Request.QueryString.Value);

                if (!Request.Query.Any())
                {
                    _logger.LogWarning("[VNPAY IPN] Request rỗng!");
                    return Ok(new VNPayReturnDto
                    {
                        RspCode = "99",
                        Message = "Empty request"
                    });
                }

                var result = await _vnPayService.VerifyPaymentProcessingAsync(Request.Query);

                if (result == null)
                {
                    return Ok(new VNPayReturnDto
                    {
                        RspCode = "99",
                        Message = "Verification failed or processing error"
                    });
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VNPAY IPN] Lỗi exception bung bét khi nhận Webhook");
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
    }
}
