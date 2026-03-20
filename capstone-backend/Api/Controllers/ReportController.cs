using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class ReportController : BaseController
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReportById(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);

        if (report == null)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse(report);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveReport(int id)
    {
        var result = await _reportService.ApproveReportAsync(id);

        if (!result)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse("Report đã được approve thành công");
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectReport(int id)
    {
        var result = await _reportService.RejectReportAsync(id);

        if (!result)
            return NotFoundResponse("Report không tồn tại");

        return OkResponse("Report đã được reject thành công");
    }
}
