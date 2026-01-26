using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "MEMBER")]
    public class PersonalityTestController : BaseController
    {
        private readonly IQuestionService _questionService;

        public PersonalityTestController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("{testTypeId:int}/questions")]
        public async Task<IActionResult> GetQuestions(int testTypeId)
        {
            try
            {
                var result = _questionService.GetAllQuestionsForMemberAsync(testTypeId);
                return OkResponse(result);
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }
    }
}
