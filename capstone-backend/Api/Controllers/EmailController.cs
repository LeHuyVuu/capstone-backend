using capstone_backend.Business.DTOs.Email;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : BaseController
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request, CancellationToken ct)
        {
            var result = await _emailService.SendEmailAsync(request, ct);
            if (result)
            {
                return OkResponse(result, "Gửi email thành công.");
            }
            else
            {
                return BadRequestResponse(result, "Gửi email thất bại. Vui lòng kiểm tra lại yêu cầu và thử lại.");
            }
        }
    }
}
