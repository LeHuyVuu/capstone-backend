using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "MEMBER")]
    public class DatePlanController : BaseController
    {
        private readonly IDatePlanService _datePlanService;

        public DatePlanController(IDatePlanService datePlanService)
        {
            _datePlanService = datePlanService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDatePlan([FromBody] CreateDatePlanRequest request)
        {

            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.CreateDatePlanAsync(userId.Value, request);

                return OkResponse(result, "Created date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
