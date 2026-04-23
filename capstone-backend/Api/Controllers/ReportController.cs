using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : BaseController
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Lấy tất cả report types đang active
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetAllReportTypes()
    {
        var reportTypes = await _reportService.GetAllReportTypesAsync();
        return OkResponse(reportTypes);
    }

    /// <summary>
    /// Member/VenueOnwer (review) tạo report mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return UnauthorizedResponse("Không thể xác định người dùng");

        try
        {
            var report = await _reportService.CreateReportAsync(request, currentUserId.Value);
            return CreatedResponse(report, "Report đã được tạo thành công và đang chờ admin kiểm duyệt");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Venue owner tố cáo review thuộc venue của mình
    /// </summary>
    [HttpPost("venue-owner/reviews/{reviewId:int}")]
    [Authorize(Roles = "VENUEOWNER,VENUE_OWNER")]
    public async Task<IActionResult> CreateVenueOwnerReviewReport(int reviewId, [FromBody] CreateVenueOwnerReviewReportRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return UnauthorizedResponse("Không thể xác định người dùng");

        try
        {
            var report = await _reportService.CreateVenueOwnerReviewReportAsync(reviewId, request, currentUserId.Value);
            return CreatedResponse(report, "Tố cáo review thành công và đang chờ admin kiểm duyệt");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResponse(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Admin lấy danh sách reports
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetReports([FromQuery] GetReportsRequest request)
    {
        var (reports, totalCount) = await _reportService.GetReportsAsync(request);

        return OkResponse(new
        {
            Reports = reports,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        });
    }

    /// <summary>
    /// Admin lấy chi tiết report
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetReportById(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);

        if (report == null)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse(report);
    }

    /// <summary>
    /// Admin approve report
    /// </summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ApproveReport(int id)
    {
        var result = await _reportService.ApproveReportAsync(id);

        if (!result)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse("Report đã được approve thành công");
    }

    /// <summary>
    /// Admin reject report
    /// </summary>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RejectReport(int id)
    {
        var result = await _reportService.RejectReportAsync(id);

        if (!result)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse("Report đã được reject thành công");
    }
}
