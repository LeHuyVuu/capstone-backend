using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : BaseController
    {
        private readonly ITestTypeService _testTypeService;

        public AdminController(ITestTypeService testTypeService)
        {
            _testTypeService = testTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoleAdmin()
        {
            if (GetCurrentUserRole() != "ADMIN")
                return ForbiddenResponse("You do not have permission to access this resource");
            else 
                return OkResponse("You are an admin");
        }

        [HttpPost("test-type")]
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
    }
}
