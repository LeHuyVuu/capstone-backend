using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/venue-owner/dashboard")]
public class VenueOwnerDashboardController : BaseController
{
    private readonly IVenueOwnerDashboardService _dashboardService;
    private readonly ILogger<VenueOwnerDashboardController> _logger;
    private readonly IRedisService _redisService;

    public VenueOwnerDashboardController(
        IVenueOwnerDashboardService dashboardService,
        ILogger<VenueOwnerDashboardController> logger,
        IRedisService redisService)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _redisService = redisService;
    }

    /// <summary>
    /// Lấy dashboard overview cho venue owner
    /// </summary>
    /// <remarks>
    /// API này trả về tổng quan dashboard cho venue owner bao gồm:
    /// 
    /// **Overview Metrics:**
    /// - Tổng số venues, active/inactive venues
    /// - Average rating, tổng reviews, check-ins, favorites
    /// 
    /// **Voucher Metrics:**
    /// - Tổng vouchers, active vouchers
    /// - Voucher exchange rate và usage rate
    /// - Số lượng vouchers đã đổi và đã dùng
    /// 
    /// **Advertisement Metrics:**
    /// - Tổng số advertisements, active/pending/rejected ads
    /// - Danh sách 5 advertisements gần nhất với thông tin chi tiết
    /// 
    /// **Engagement Metrics:**
    /// - Số lần được thêm vào date plans
    /// - Số lần được lưu vào collections
    /// - Unique customers và returning customers
    /// 
    /// **Recent Activity:**
    /// - Reviews và check-ins trong tuần/tháng này
    /// 
    /// **Growth Metrics:**
    /// - Tỷ lệ tăng trưởng reviews và check-ins so với tháng trước
    /// 
    /// **Top Performing Venue:**
    /// - Venue có engagement cao nhất
    /// 
    /// **Venues List:**
    /// - Danh sách tất cả venues với performance metrics
    /// </remarks>
    /// <response code="200">Trả về dashboard overview</response>
    /// <response code="401">Chưa đăng nhập hoặc không phải venue owner</response>
    /// <response code="403">Không có quyền truy cập</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDashboardOverview()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("User không xác thực");
            }

            // Cache key dựa trên userId
            var cacheKey = $"venue_owner:dashboard:overview:{userId.Value}";

            // Sử dụng Redis cache với GetOrSetAsync - tự động handle cache miss
            var dashboard = await _redisService.GetOrSetAsync(
                cacheKey,
                async () => await _dashboardService.GetDashboardOverviewAsync(userId.Value),
                TimeSpan.FromMinutes(5) 
            );

            return OkResponse(dashboard, "Lấy dashboard overview thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview for user {UserId}", GetCurrentUserId());
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy dashboard overview");
        }
    }

    /// <summary>
    /// Lấy chi tiết analytics cho 1 venue cụ thể
    /// </summary>
    /// <param name="venueId">ID của venue cần xem analytics</param>
    /// <param name="days">Số ngày lấy data (mặc định 30 ngày)</param>
    /// <remarks>
    /// API này trả về chi tiết analytics cho 1 venue bao gồm:
    /// 
    /// **Rating Distribution:**
    /// - Phân bố rating từ 1-5 sao
    /// - Average rating, tổng reviews
    /// - Reviews có ảnh, reviews từ couples
    /// 
    /// **Time-series Data:**
    /// - Review trend theo ngày
    /// - Check-in trend theo ngày
    /// 
    /// **Customer Insights:**
    /// - Unique customers, returning customers
    /// - Couple vs single customers
    /// - Return rate và couple rate
    /// 
    /// **Peak Hours:**
    /// - Giờ cao điểm check-in
    /// 
    /// **Recent Reviews:**
    /// - 10 reviews mới nhất với thông tin chi tiết
    /// 
    /// **Voucher Performance:**
    /// - Voucher metrics cho venue này
    /// - Top vouchers được đổi nhiều nhất
    /// </remarks>
    /// <response code="200">Trả về venue analytics</response>
    /// <response code="401">Chưa đăng nhập hoặc không có quyền xem venue này</response>
    /// <response code="404">Venue không tồn tại</response>
    [HttpGet("venues/{venueId}/analytics")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVenueAnalytics(
        [FromRoute] int venueId,
        [FromQuery] int days = 30)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("User không xác thực");
            }

            // Validate days parameter
            if (days < 1 || days > 365)
            {
                return BadRequestResponse("Days phải từ 1 đến 365");
            }

            // Cache key bao gồm userId, venueId và days
            var cacheKey = $"venue_owner:venue:analytics:{userId.Value}:{venueId}:{days}";

            // Sử dụng Redis cache với GetOrSetAsync
            var analytics = await _redisService.GetOrSetAsync(
                cacheKey,
                async () => await _dashboardService.GetVenueAnalyticsAsync(userId.Value, venueId, days),
                TimeSpan.FromMinutes(5) // Cache 5 phút
            );

            return OkResponse(analytics, "Lấy venue analytics thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue analytics for venue {VenueId}", venueId);
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy venue analytics");
        }
    }
}
