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
    [Authorize]
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
        /// Get All Test Types
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestTypes()
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role != "ADMIN" && role != "MEMBER")
                    return ForbiddenResponse("You do not have permission to access this resource");

                var response = new List<TestTypeResponse>();

                if (role == "ADMIN")
                {
                    response = await _testTypeService.GetAllTestTypeAsync(role);
                }
                else
                {
                    response = await _testTypeService.GetAllTestTypeAsync();
                }

                    return OkResponse(response, "Test types retrieved successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Test Type
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> TestType(int id)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role != "ADMIN" && role != "MEMBER")
                    return ForbiddenResponse("You do not have permission to access this resource");

                var response = new TestTypeDetailDto();

                if (role == "ADMIN")
                {
                    response = await _testTypeService.GetByIdAsync(id, role);
                }
                else
                {
                    response = await _testTypeService.GetByIdAsync(id);
                }

                return OkResponse(response, "Test type retrieved successfully");
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
                    return ForbiddenResponse("You do not have permission to access this resource");

                var response = await _testTypeService.CreateTestTypeAsync(request);
                if (response > 0)
                    return CreatedResponse("Test type created successfully");
                else
                    return BadRequestResponse("Failed to create test type");
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
                    return ForbiddenResponse("You do not have permission to access this resource");
                var response = await _testTypeService.UpdateTestTypeAsync(id, request);
                if (response > 0)
                    return OkResponse("Test type updated successfully");
                else
                    return BadRequestResponse("Failed to update test type");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete Test Type (Admin only)
        /// </summary>
        [HttpPatch("{id:int}/delete")]
        public async Task<IActionResult> DeleteTestType(int id)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("You do not have permission to access this resource");
                var response = await _testTypeService.DeleteTestTypeAsync(id);
                if (response > 0)
                    return OkResponse("Test type deleted successfully");
                else
                    return BadRequestResponse("Failed to delete test type");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Import Question for Test Type (Admin only)
        /// </summary>
        [HttpPost("{testTypeId:int}/question/import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GenerateQuestions([FromRoute] int testTypeId, [FromForm] GenerateQuestionsRequest request, CancellationToken ct)
        {
            try
            {
                var isAdmin = IsCurrentUserInRole("ADMIN");
                if (!isAdmin)
                    return ForbiddenResponse("You do not have permission to access this resource");

                if (request.File == null || request.File.Length == 0)
                    return BadRequest("File is required.");               

                var result = await _questionService.GenerateQuestionAsync(
                    testTypeId,
                    request.File,
                    ct
                );

                return result.Errors.Any()
                    ? BadRequest(result)
                    : Ok(result);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
