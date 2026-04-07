using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Report;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportTypeController : BaseController
{
    private readonly IReportTypeService _reportTypeService;

    public ReportTypeController(IReportTypeService reportTypeService)
    {
        _reportTypeService = reportTypeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReportTypeResponse>>), 200)]
    public async Task<IActionResult> GetReportTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        if (page < 1)
            return BadRequestResponse("Số trang phải lớn hơn 0");

        if (pageSize < 1 || pageSize > 100)
            return BadRequestResponse("Kích thước trang phải trong khoảng từ 1 đến 100");

        var result = await _reportTypeService.GetReportTypesAsync(page, pageSize, isActive);
        return OkResponse(result, $"Retrieved {result.Items.Count()} report types");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportTypeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetReportTypeById(int id)
    {
        var reportType = await _reportTypeService.GetReportTypeByIdAsync(id);

        if (reportType == null)
            return NotFoundResponse($"Không tìm thấy loại báo cáo có ID {id}");

        return OkResponse(reportType, "Report type retrieved successfully");
    }
    [Authorize(Roles = "ADMIN")]

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReportTypeResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateReportType([FromBody] CreateReportTypeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");

        var reportType = await _reportTypeService.CreateReportTypeAsync(request);
        return CreatedResponse(reportType, "Report type created successfully");
    }
[Authorize(Roles = "ADMIN")]

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportTypeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateReportType(int id, [FromBody] UpdateReportTypeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequestResponse("Dữ liệu yêu cầu không hợp lệ");

        var reportType = await _reportTypeService.UpdateReportTypeAsync(id, request);

        if (reportType == null)
            return NotFoundResponse($"Không tìm thấy loại báo cáo có ID {id}");

        return OkResponse(reportType, "Report type updated successfully");
    }
[Authorize(Roles = "ADMIN")]

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteReportType(int id)
    {
        var result = await _reportTypeService.DeleteReportTypeAsync(id);

        if (!result)
            return NotFoundResponse($"Không tìm thấy loại báo cáo có ID {id}");

        return OkResponse(true, "Report type deleted successfully");
    }
}
