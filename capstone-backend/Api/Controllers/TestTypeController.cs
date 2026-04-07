using capstone_backend.Business.DTOs.Question;
using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class TestTypeController : BaseController
    {
        private readonly ITestTypeService _testTypeService;
        private readonly IQuestionService _questionService;

        public TestTypeController(ITestTypeService testTypeService, IQuestionService questionService)
        {
            _testTypeService = testTypeService;
            _questionService = questionService;
        }

        /// <summary>
        /// Get All Test Types (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestTypes()
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role != "ADMIN")
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");

                var response = await _testTypeService.GetAllTestTypeAsync();

                return OkResponse(response, "Lấy danh sách loại bài test thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Test Type (Admin only)
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> TestType(int id)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role != "ADMIN")
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");

                var response = await _testTypeService.GetByIdAsync(id);

                return OkResponse(response, "Lấy thông tin loại bài test thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Create Test Type (Admin only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestType([FromBody] CreateTestTypeResquest request)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");

                var response = await _testTypeService.CreateTestTypeAsync(request);
                if (response > 0)
                    return CreatedResponse("Tạo loại bài test thành công");
                else
                    return BadRequestResponse("Tạo loại bài test thất bại");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update Test Type (Admin only)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTestType(int id, [FromBody] UpdateTestTypeRequest request)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");
                var response = await _testTypeService.UpdateTestTypeAsync(id, request);
                if (response > 0)
                    return OkResponse("Cập nhật loại bài test thành công");
                else
                    return BadRequestResponse("Cập nhật loại bài test thất bại");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete Test Type (Admin only)
        /// </summary>
        [HttpDelete("{id:int}/delete")]
        public async Task<IActionResult> DeleteTestType(int id)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");
                var response = await _testTypeService.DeleteTestTypeAsync(id);
                if (response > 0)
                    return OkResponse("Xóa loại bài test thành công");
                else
                    return BadRequestResponse("Xóa loại bài test thất bại");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Import Question for Test Type (Admin only)
        /// </summary>
        [HttpPost("{id:int}/question/import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerateQuestions([FromRoute] int id, [FromForm] GenerateQuestionsRequest request, CancellationToken ct)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");

                if (request.File == null || request.File.Length == 0)
                    return BadRequest("Tệp là bắt buộc.");

                var result = await _questionService.GenerateQuestionAsync(
                    id,
                    request.File,
                    ct
                );

                return result.Errors.Any()
                    ? BadRequestResponse(result)
                    : OkResponse(result);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Get All Questions for Test Type (Admin only)
        /// </summary>
        [HttpGet("{id:int}/question")]
        public async Task<IActionResult> GetQuestionsByTestTypeAndVersion(int id, [FromQuery] int version)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role != "ADMIN")
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");

                var questions = await _questionService.GetAllQuestionsByVersionAsync(id, version);
                return OkResponse(questions, "Lấy danh sách câu hỏi thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Activate Question Version for Test Type (Admin only)
        /// </summary>
        [HttpPatch("{id:int}/activate-version")]
        public async Task<IActionResult> ActivateQuestionVersion(int id, [FromQuery] int version)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("Bạn không có quyền truy cập tài nguyên này");
                var response = await _questionService.ActivateVersionAsync(id, version);
                if (response > 0)
                    return OkResponse("Kích hoạt phiên bản câu hỏi thành công");
                else
                    return BadRequestResponse("Kích hoạt phiên bản câu hỏi thất bại");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
