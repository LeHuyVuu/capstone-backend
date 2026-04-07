using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Advertisement;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdvertisementController : BaseController
{
    private readonly IAdvertisementService _advertisementService;
    private readonly ILogger<AdvertisementController> _logger;

    public AdvertisementController(
        IAdvertisementService advertisementService,
        ILogger<AdvertisementController> logger)
    {
        _advertisementService = advertisementService;
        _logger = logger;
    }


    /// <param name="placementType">Optional: Filter by placement type (HOME_BANNER, POPUP, AND leave blank is random to display in carousel etc.)</param>
    /// <returns>List of rotating advertisements and special events with venue information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AdvertisementResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetRotatingAdvertisements([FromQuery] string? placementType = null)
    {
        _logger.LogInformation("Member requesting rotating advertisements (PlacementType: {PlacementType})", 
            placementType ?? "all");

        try
        {
            var advertisements = await _advertisementService.GetRotatingAdvertisementsAsync(placementType);

            var adCount = advertisements.Count(a => a.Type == "ADVERTISEMENT");
            var eventCount = advertisements.Count(a => a.Type == "SPECIAL_EVENT");

            return OkResponse(advertisements,
                $"Retrieved {advertisements.Count} item(s): {adCount} advertisement(s) + {eventCount} special event(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rotating advertisements");
            return BadRequestResponse("Không thể lấy danh sách quảng cáo");
        }
    }

    /// <summary>
    /// Grouped by placement type (HOME_BANNER, POPUP, etc.)
    /// </summary>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(ApiResponse<GroupedAdvertisementPackagesResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAdvertisementPackages()
    {
        _logger.LogInformation("Requesting all active advertisement packages");

        try
        {
            var result = await _advertisementService.GetAdvertisementPackagesAsync();
            var totalCount = result.Data.Values.Sum(list => list.Count);
            var placementCount = result.Data.Count;
            
            return OkResponse(result, 
                $"Retrieved {placementCount} placement group(s) with {totalCount} active package(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisement packages");
            return BadRequestResponse("Không thể lấy danh sách gói quảng cáo");
        }
    }


    /// <param name="request">Advertisement creation request</param>
    /// <returns>Created advertisement detail</returns>
    [HttpPost("create")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateAdvertisement([FromBody] CreateAdvertisementRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        try
        {
            var advertisement = await _advertisementService.CreateAdvertisementAsync(request, currentUserId.Value);
            return CreatedResponse(advertisement, "Tạo quảng cáo ở trạng thái DRAFT thành công");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return BadRequestResponse("Không thể tạo quảng cáo");
        }
    }


    [HttpPut("{id}/update-and-draft")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateAdvertisementAndRevertToDraft(int id, [FromBody] UpdateAdvertisementRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        try
        {
            var advertisement = await _advertisementService.UpdateAdvertisementAndRevertToDraftAsync(id, currentUserId.Value, request);
            return OkResponse(advertisement, "Cập nhật quảng cáo thành công và đã chuyển về trạng thái DRAFT. Bạn có thể gửi lại kèm thanh toán.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return BadRequestResponse("Không thể cập nhật quảng cáo");
        }
    }


    [HttpGet("my-advertisements")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<List<MyAdvertisementResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetMyAdvertisements()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        _logger.LogInformation("VenueOwner {UserId} requesting their advertisements", currentUserId.Value);

        try
        {
            var advertisements = await _advertisementService.GetMyAdvertisementsAsync(currentUserId.Value);
            return OkResponse(advertisements, $"Đã lấy {advertisements.Count} quảng cáo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisements for user {UserId}", currentUserId);
            return BadRequestResponse("Không thể lấy danh sách quảng cáo");
        }
    }

    /// <summary>
    /// Get ads orders for venue owner with detailed payment and status information
    /// </summary>
    [HttpGet("my-ads-orders")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<List<AdsOrderResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetMyAdsOrders([FromQuery] string? status = null)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        _logger.LogInformation("VenueOwner {UserId} requesting their ads orders (Status: {Status})", 
            currentUserId.Value, status ?? "all");

        try
        {
            var adsOrders = await _advertisementService.GetMyAdsOrdersAsync(currentUserId.Value, status);
            return OkResponse(adsOrders, $"Đã lấy {adsOrders.Count} đơn quảng cáo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ads orders for user {UserId}", currentUserId);
            return BadRequestResponse("Không thể lấy danh sách đơn quảng cáo");
        }
    }


    /// <param name="id">Advertisement ID</param>
    /// <returns>Advertisement detail</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAdvertisementById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        _logger.LogInformation("VenueOwner {UserId} requesting advertisement detail for ID: {AdId}", currentUserId, id);

        try
        {
            var advertisement = await _advertisementService.GetAdvertisementByIdAsync(id, currentUserId.Value);

            if (advertisement == null)
            {
                return NotFoundResponse($"Không tìm thấy quảng cáo có ID {id} hoặc bạn không có quyền xem");
            }

            return OkResponse(advertisement, "Lấy thông tin quảng cáo thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisement {AdId} for user {UserId}", id, currentUserId);
            return BadRequestResponse("Không thể lấy thông tin quảng cáo");
        }
    }

    /// <param name="id">Advertisement ID</param>
    /// <param name="request">Payment request with packageId</param>
    /// <returns>Payment QR code and transaction info</returns>
    [HttpPost("{id}/submit-with-payment")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<SubmitAdvertisementWithPaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SubmitAdvertisementWithPayment(int id, [FromBody] SubmitAdvertisementWithPaymentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        _logger.LogInformation("VenueOwner {UserId} submitting advertisement {AdId} with payment", currentUserId, id);

        try
        {
            var result = await _advertisementService.SubmitAdvertisementWithPaymentAsync(id, currentUserId.Value, request);

            if (!result.IsSuccess)
            {
                // Return validation errors with 200 but IsSuccess = false
                return OkResponse(result, result.Message);
            }

            return OkResponse(result, "Đã tạo mã QR thanh toán. Vui lòng quét để hoàn tất thanh toán.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation submitting advertisement {AdId} for user {UserId}", id, currentUserId);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting advertisement {AdId} with payment for user {UserId}", id, currentUserId);
            return BadRequestResponse("Không thể gửi quảng cáo kèm thanh toán");
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<List<MyAdvertisementResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetPendingAdvertisements()
    {
        try
        {
            var advertisements = await _advertisementService.GetPendingAdvertisementsAsync();
            return OkResponse(advertisements, $"Đã lấy {advertisements.Count} quảng cáo đang chờ duyệt");
        }
        catch (Exception)
        {
            return BadRequestResponse("Không thể lấy danh sách quảng cáo đang chờ duyệt");
        }
    }

    [HttpPost("approve")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ApproveAdvertisement([FromBody] ApproveAdvertisementRequest request)
    {
        _logger.LogInformation("Admin approving advertisement {AdId}", request.AdvertisementId);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");
        }

        var result = await _advertisementService.ApproveAdvertisementAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Message == "Advertisement not found") 
                return NotFoundResponse(result.Message);
            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }

    [HttpPost("reject")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RejectAdvertisement([FromBody] RejectAdvertisementRequest request)
    {
        _logger.LogInformation("Admin rejecting advertisement {AdId}", request.AdvertisementId);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");
        }

        var result = await _advertisementService.RejectAdvertisementAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Message == "Advertisement not found") 
                return NotFoundResponse(result.Message);
            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }

    [HttpDelete("{id}/soft-delete")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SoftDeleteAdvertisement(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        var result = await _advertisementService.SoftDeleteAdvertisementAsync(id, currentUserId.Value);
        if (!result)
        {
            return NotFoundResponse($"Không tìm thấy quảng cáo có ID {id} hoặc bạn không có quyền xóa");
        }

        return OkResponse(true, "Xóa mềm quảng cáo thành công");
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RestoreAdvertisement(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        }

        var result = await _advertisementService.RestoreAdvertisementAsync(id, currentUserId.Value);
        if (!result.IsSuccess)
        {
            if (result.Message == "Advertisement not found")
            {
                return NotFoundResponse(result.Message);
            }

            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }

    [HttpDelete("admin/{id}/hard-delete")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> HardDeleteAdvertisement(int id)
    {
        var result = await _advertisementService.HardDeleteAdvertisementAsync(id);
        if (!result)
        {
            return NotFoundResponse($"Không tìm thấy quảng cáo có ID {id}");
        }

        return OkResponse(true, "Xóa vĩnh viễn quảng cáo thành công");
    }

    #region Public Detail Endpoints


    /// <param name="id">Advertisement ID or Special Event ID</param>
    /// <param name="type">Type: ADVERTISEMENT or SPECIAL_EVENT</param>
    [HttpGet("detail/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] string type)
    {
        _logger.LogInformation("Public request for {Type} detail ID {Id}", type, id);

        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequestResponse("Tham số loại là bắt buộc (ADVERTISEMENT hoặc SPECIAL_EVENT)");
        }

        var typeUpper = type.Trim().ToUpper();

        try
        {
            if (typeUpper == "ADVERTISEMENT")
            {
                var detail = await _advertisementService.GetPublicAdvertisementDetailAsync(id);
                return OkResponse(detail, "Lấy chi tiết quảng cáo thành công");
            }
            else if (typeUpper == "SPECIAL_EVENT")
            {
                var detail = await _advertisementService.GetSpecialEventDetailAsync(id);
                return OkResponse(detail, "Lấy chi tiết sự kiện đặc biệt thành công");
            }
            else
            {
                return BadRequestResponse($"Loại '{type}' không hợp lệ. Chỉ chấp nhận ADVERTISEMENT hoặc SPECIAL_EVENT");
            }
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("{Type} {Id} not found: {Message}", typeUpper, id, ex.Message);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Type} detail for ID {Id}", typeUpper, id);
            return BadRequestResponse($"Không thể lấy chi tiết {typeUpper.ToLower()}");
        }
    }

    #endregion
}
