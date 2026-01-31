using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
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

        /// <summary>
        /// Get All Date Plans By Time
        /// </summary>
        /// <remarks>
        /// UPCOMMING - Get all date plans that are upcoming
        /// 
        /// PAST - Get all date plans that are past
        /// 
        /// ALL - Get all date plans (default)
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetDatePlans(
            [FromQuery] DatePlanTime time = DatePlanTime.ALL, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 5)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.GetAllDatePlansByTimeAsync(pageNumber, pageSize, userId.Value, time.ToString());

                return OkResponse(result, "Fetched date plans successfully");
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Detail Date Plan
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetailDatePlan(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.GetByIdAsync(id, userId.Value);
                return OkResponse(result, "Fetched date plan detail successfully");
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Create Date Plan
        /// </summary>
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

        /// <summary>
        /// Add venue to Date Plan
        /// </summary>
        [HttpPost("{datePlanId:int}/items")]
        public async Task<IActionResult> AddVenuesToDatePlan(int datePlanId, CreateDatePlanItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.AddVenuesToDatePlanAsync(userId.Value, datePlanId, request);
                return OkResponse(result, "Added venues to date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
