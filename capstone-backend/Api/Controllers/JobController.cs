using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class JobController : BaseController
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<JobController> _logger;

    public JobController(
        IWebHostEnvironment webHostEnvironment,
        ILogger<JobController> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    /// <summary>
    /// Get list of job titles
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetJobs()
    {
        try
        {
            var resourcePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Resources", "Job", "Job.json");

            if (!System.IO.File.Exists(resourcePath))
            {
                _logger.LogError("Job resource file not found at: {Path}", resourcePath);
                return InternalServerErrorResponse("Không tìm thấy tệp dữ liệu nghề nghiệp");
            }

            var jsonContent = await System.IO.File.ReadAllTextAsync(resourcePath);
            var jobData = JsonSerializer.Deserialize<JobListResponse>(jsonContent);

            if (jobData?.Jobs == null || !jobData.Jobs.Any())
            {
                _logger.LogError("Failed to parse job data from JSON");
                return InternalServerErrorResponse("Không thể phân tích dữ liệu nghề nghiệp");
            }

            return OkResponse(jobData.Jobs, $"Lấy {jobData.Jobs.Count} nghề nghiệp thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            return InternalServerErrorResponse($"Lỗi khi lấy danh sách nghề nghiệp: {ex.Message}");
        }
    }

    private class JobListResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("jobs")]
        public List<string>? Jobs { get; set; }
    }
}
