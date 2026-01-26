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
        private readonly ITestTypeService _testTypeService;

        public PersonalityTestController(IQuestionService questionService, ITestTypeService testTypeService)
        {
            _questionService = questionService;
            _testTypeService = testTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTestTypes()
        {
            try
            {
                var result = await _testTypeService.GetAllActiveAsync();
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        [HttpGet("{testTypeId:int}/questions")]
        public async Task<IActionResult> GetQuestions(int testTypeId)
        {
            try
            {
                var result = await _questionService.GetAllQuestionsForMemberAsync(testTypeId);
                return OkResponse(result);
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }
    }
}
