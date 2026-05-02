using capstone_backend.Business.DTOs.VenueSettlement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "VENUEOWNER, venueowner")]
    [ApiController]
    public class VenueSettlementController : BaseController
    {
        private readonly IVenueSettlementService _venueSettlementService;
        private readonly ISystemConfigService _systemConfigService;

        public VenueSettlementController(IVenueSettlementService venueSettlementService, ISystemConfigService systemConfigService)
        {
            _venueSettlementService = venueSettlementService;
            _systemConfigService = systemConfigService;
        }

        /// <summary>
        /// Get commission percent for venue owner
        /// </summary>
        [HttpGet("commission")]
        public async Task<IActionResult> GetCommissionPercent()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();
                var result = await _systemConfigService.GetByKeyAsync(SystemConfigKeys.VENUE_COMMISSION_PERCENT.ToString());
                return OkResponse(result, "Lấy phần trăm hoa hồng thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get revenue settlement of vouchers
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueSettlement([FromQuery] RevenueRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();
                var result = await _venueSettlementService.GetRevenueAsync(userId.Value, request);
                return OkResponse(result, "Lấy doanh thu đối soát thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get summary settlement of vouchers
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummarySettlement()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();

                var result = await _venueSettlementService.GetSummaryAsync(userId.Value);
                return OkResponse(result, "Lấy tổng quan đối soát thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get settlement list for venue owner
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSettlements([FromQuery] GetVenueSettlementsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();

                var result = await _venueSettlementService.GetSettlementsAsync(userId.Value, request);
                return OkResponse(result, "Lấy danh sách đối soát thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get settlement detail for venue owner
        /// </summary>
        [HttpGet("{settlementId}")]
        public async Task<IActionResult> GetSettlementDetail(int settlementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return UnauthorizedResponse();

                var result = await _venueSettlementService.GetSettlementDetailAsync(userId.Value, settlementId);
                return OkResponse(result, "Lấy chi tiết đối soát thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
