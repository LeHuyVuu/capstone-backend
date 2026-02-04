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
    [Authorize(Roles = "MEMBER, member")]
    public class DatePlanController : BaseController
    {
        private readonly IDatePlanService _datePlanService;
        private readonly IDatePlanItemService _datePlanItemService;

        public DatePlanController(IDatePlanService datePlanService, IDatePlanItemService datePlanItemService)
        {
            _datePlanService = datePlanService;
            _datePlanItemService = datePlanItemService;
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
                var (result, totalUpcoming) = await _datePlanService.GetAllDatePlansByTimeAsync(pageNumber, pageSize, userId.Value, time.ToString());

                var customReponse = new DatePlanPagedResponse
                {
                    PagedResult = result,
                    TotalUpcoming = totalUpcoming
                };

                return OkResponse(customReponse, "Fetched date plans successfully");
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get All Date Plans Items
        /// </summary>
        [HttpGet("{datePlanId:int}/items")]
        public async Task<IActionResult> GetDatePlanItems(
            int datePlanId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.GetAllAsync(pageNumber, pageSize, userId.Value, datePlanId);
                return OkResponse(result, "Fetched date plan items successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Detail Date Plan
        /// </summary>
        [HttpGet("{datePlanId:int}")]
        public async Task<IActionResult> GetDetailDatePlan(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.GetByIdAsync(datePlanId, userId.Value);
                return OkResponse(result, "Fetched date plan detail successfully");
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Detail Date Plan Item
        /// </summary>
        [HttpGet("{datePlanId:int}/items/{datePlanItemId:int}")]
        public async Task<IActionResult> GetDetailDatePlanItem(int datePlanId, int datePlanItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.GetDetailDatePlanItemAsync(userId.Value, datePlanItemId, datePlanId);
                return OkResponse(result, "Fetched date plan item detail successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Start Date Plan
        /// </summary>
        

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
                var result = await _datePlanItemService.AddVenuesToDatePlanAsync(userId.Value, datePlanId, request);
                return OkResponse(result, "Added venues to date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update Date Plan
        /// </summary>
        [HttpPatch("{datePlanId:int}")]
        public async Task<IActionResult> UpdateDatePlan(int datePlanId,
            [FromQuery] int version,
            [FromBody] UpdateDatePlanRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.UpdateDatePlanAsync(userId.Value, datePlanId, version, request);
                return OkResponse(result, "Updated date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update Date Plan Item
        /// </summary>
        [HttpPatch("{datePlanId:int}/items/{datePlanItemId:int}")]
        public async Task<IActionResult> UpdateDatePlanItem(
            int datePlanId,
            int datePlanItemId,
            [FromQuery] int version,
            UpdateDatePlanItemRequest request
        )
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.UpdateItemAsync(userId.Value, datePlanId, datePlanItemId, version, request);
                return OkResponse(result, "Updated date plan item successfully");
            }
            catch (Exception ex)
            {

                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete Date Plan
        /// </summary>
        [HttpDelete("{datePlanId:int}")]
        public async Task<IActionResult> DeleteDatePlan(
            int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.DeleteDatePlanAsync(userId.Value, datePlanId);
                if (result <= 0)
                {
                    return BadRequestResponse("Failed to delete date plan");
                }
                return OkResponse(result, "Deleted date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete Date Plan Item
        /// </summary>
        [HttpDelete("{datePlanId:int}/items/{datePlanItemId:int}")]
        public async Task<IActionResult> DeleteDatePlanItem(
            int datePlanId,
            int datePlanItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.DeleteDatePlanItemAsync(userId.Value, datePlanItemId, datePlanId);
                if (result <= 0)
                {
                    return BadRequestResponse("Failed to delete date plan item");
                }
                return OkResponse(result, "Deleted date plan successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
