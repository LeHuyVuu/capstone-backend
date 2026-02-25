using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModerationController : ControllerBase
    {
        private readonly IModerationService _moderationService;

        public ModerationController(IModerationService moderationService)
        {
            _moderationService = moderationService;
        }

        [HttpGet]
        public async Task<IActionResult> CheckContent([FromQuery] string content)
        {
            var result = await _moderationService.CheckContentByAIService(new List<string> { content });
            return Ok(new
            {
                StatusCode = 200,
                Content = content,
                Result = result
            });
        }
    }
}
