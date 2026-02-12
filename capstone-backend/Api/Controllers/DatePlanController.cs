using capstone_backend.Api.Filters;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "MEMBER, member")]
    [Moderation]
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
        /// DRAFTED - Get all drafted date plans
        /// 
        /// PENDING - Get all pending date plans (pending approval)
        /// 
        /// ALL - Get all date plans (default)
        /// </remarks>
        [HttpGet]
        public async Task<IActionResult> GetDatePlans(
            [FromQuery] DatePlanTime time = DatePlanTime.ALL,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
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

                return OkResponse(customReponse, "Lấy danh sách lịch trình buổi hẹn thành công");
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
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.GetAllAsync(pageNumber, pageSize, userId.Value, datePlanId);
                return OkResponse(result, "Lấy danh sách mục lịch trình thành công");
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
                return OkResponse(result, "Lấy chi tiết 1 lịch trình buổi hẹn thành công");
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
                return OkResponse(result, "Lấy chi tiết 1 mục lịch trình thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Send Date Plan
        /// </summary>
        [HttpPatch("{datePlanId:int}/send")]
        public async Task<IActionResult> SendDatePlanToCoupleMember(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.ActionDatePlanAsync(userId.Value, datePlanId, DatePlanAction.SEND);
                return OkResponse(result, "Gửi lịch trình buổi hẹn cho nửa kia thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Accept Date Plan
        /// </summary>
        [HttpPatch("{datePlanId:int}/accept")]
        public async Task<IActionResult> AcceptDatePlanToCoupleMember(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.ActionDatePlanAsync(userId.Value, datePlanId, DatePlanAction.ACCEPT);
                return OkResponse(result, "Chấp nhận lịch trình buổi hẹn từ nửa kia thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Reject Date Plan
        /// </summary>
        [HttpPatch("{datePlanId:int}/reject")]
        public async Task<IActionResult> RejectDatePlanToCoupleMember(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.ActionDatePlanAsync(userId.Value, datePlanId, DatePlanAction.REJECT);
                return OkResponse(result, "Từ chối lịch trình buổi hẹn từ nửa kia thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Cancel Date Plan (when member want to cancel early)
        /// </summary>
        [HttpPatch("{datePlanId:int}/cancel")]
        public async Task<IActionResult> CancelDatePlanToCoupleMember(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.ActionDatePlanAsync(userId.Value, datePlanId, DatePlanAction.CANCEL);
                return OkResponse(result, "Huỷ lịch trình buổi hẹn thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Complete Date Plan (when member want to complete early)
        /// </summary>
        [HttpPatch("{datePlanId:int}/complete")]
        public async Task<IActionResult> CompleteDatePlanToCoupleMember(int datePlanId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanService.ActionDatePlanAsync(userId.Value, datePlanId, DatePlanAction.COMPLETE);
                return OkResponse(result, "Hoàn thành lịch trình buổi hẹn thành công");
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

                return OkResponse(result, "Tạo lịch trình buổi hẹn thành công");
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
                return OkResponse(result, "Thêm địa điểm mới vào lịch trình buổi hẹn thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Reorder Date Plan Items
        /// </summary>
        [HttpPut("{datePlanId:int}/items/reorder")]
        public async Task<IActionResult> ReorderDatePlanItems(
            int datePlanId,
            [FromBody] ReorderDatePlanItemsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _datePlanItemService.ReorderDatePlanItemAsync(userId.Value, datePlanId, request);
                return OkResponse(result, "Sắp xếp địa điểm thành công");
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
                return OkResponse(result, "Cập nhật lịch trình thành công");
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
                return OkResponse(result, "Cập nhật mục lịch trình thành công");
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
                    return BadRequestResponse("Xoá lịch trình thất bại");
                }
                return OkResponse(result, "Xoá lịch trình thành công");
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
                    return BadRequestResponse("Xoá mục lịch trình thất bại");
                }
                return OkResponse(result, "Xoá mục lịch trình thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
