using capstone_backend.Api.Models;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// Controller cho venue tag analysis
/// Venue owner có thể xem phân tích tags của venue mình
/// </summary>
[ApiController]
[Route("api/venue-owner/tag-analysis")]
public class VenueTagAnalysisController : BaseController
{
    private readonly IVenueTagAnalysisService _tagAnalysisService;
    private readonly IUnitOfWork _unitOfWork;

    public VenueTagAnalysisController(
        IVenueTagAnalysisService tagAnalysisService,
        IUnitOfWork unitOfWork)
    {
        _tagAnalysisService = tagAnalysisService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Phân tích độ chính xác của tags cho venue
    /// Venue owner chỉ có thể xem phân tích của venue mình
    /// </summary>
    /// <param name="venueId">Venue ID</param>
    /// <returns>Phân tích chi tiết từng tag</returns>
    [HttpGet("{venueId}")]
    public async Task<IActionResult> AnalyzeVenueTags(int venueId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedResponse("Không xác định được user");
        }

        // Verify venue ownership
        var venue = await _unitOfWork.VenueLocations.GetByIdWithOwnerAsync(venueId);
        if (venue == null)
        {
            return NotFoundResponse("Không tìm thấy venue");
        }

        if (venue.VenueOwner.UserId != userId.Value)
        {
            return ForbiddenResponse("Bạn không có quyền xem phân tích của venue này");
        }

        var result = await _tagAnalysisService.AnalyzeVenueTagsAsync(venueId);
        return OkResponse(result, "Lấy phân tích tags thành công");
    }
}

/// <summary>
/// Admin controller để xem tag analysis của bất kỳ venue nào
/// </summary>
[ApiController]
[Route("api/admin/venue-tag-analysis")]
[Authorize(Roles = "ADMIN")]
public class AdminVenueTagAnalysisController : BaseController
{
    private readonly IVenueTagAnalysisService _tagAnalysisService;

    public AdminVenueTagAnalysisController(IVenueTagAnalysisService tagAnalysisService)
    {
        _tagAnalysisService = tagAnalysisService;
    }

    /// <summary>
    /// Admin xem phân tích tags của bất kỳ venue nào
    /// </summary>
    [HttpGet("{venueId}")]
    public async Task<IActionResult> AnalyzeVenueTags(int venueId)
    {
        var result = await _tagAnalysisService.AnalyzeVenueTagsAsync(venueId);
        return OkResponse(result, "Lấy phân tích tags thành công");
    }
}
