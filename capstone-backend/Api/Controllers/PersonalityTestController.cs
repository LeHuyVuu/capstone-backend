using capstone_backend.Business.DTOs.PersonalityTest;
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
        private readonly IPersonalityTestService _personalityTestService;

        public PersonalityTestController(IQuestionService questionService, ITestTypeService testTypeService, IPersonalityTestService personalityTestService)
        {
            _questionService = questionService;
            _testTypeService = testTypeService;
            _personalityTestService = personalityTestService;
        }

        /// <summary>
        /// Get All Test Types
        /// </summary>
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

        /// <summary>
        /// Get All History Tests
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetTestHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _personalityTestService.GetHistoryTests(pageNumber, pageSize, userId.Value);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        ///<summary>
        /// Get Detail of 1 History Test
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTestHistoryDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _personalityTestService.GetTestHistoryDetailAsync(id, userId.Value);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        ///<summary>
        /// Get My Current Personality Result
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyCurrentPersonality()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _personalityTestService.GetCurrentPersonalityAsync(userId.Value);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get All Questions of 1 test type
        /// </summary>
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

        /// <summary>
        /// Save progress or submit a personality test
        /// </summary>
        /// <remarks>
        /// 1. SAVE_PROGRESS: Lưu tạm tiến độ khi người dùng chưa làm xong bài test  
        /// 2. SUBMIT: Nộp bài test khi người dùng hoàn thành
        /// 
        /// Quy ước:
        /// - action = "SAVE_PROGRESS" → chỉ lưu, không chấm điểm
        /// - action = "SUBMIT" → BE validate đủ câu hỏi và trả kết quả
        /// 
        /// FE chỉ cần gửi:
        /// - action
        /// - currentQuestionIndex
        /// - answers (questionId, answerId)
        /// </remarks>
        [HttpPost("{testTypeId:int}/submit")]
        public async Task<IActionResult> SaveOrSubmitTest(int testTypeId, [FromBody] SaveTestResultRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _personalityTestService.HandleTestAsync(GetCurrentUserId().Value, testTypeId, request);
                return OkResponse(result, "Test submitted successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
