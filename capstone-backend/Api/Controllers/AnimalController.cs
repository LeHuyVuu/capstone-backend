using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnimalController : BaseController
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<AnimalController> _logger;

    public AnimalController(
        IWebHostEnvironment webHostEnvironment,
        ILogger<AnimalController> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    /// <summary>
    /// Get list of cute friendly animals
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetAnimals()
    {
        try
        {
            var resourcePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Resources", "Animal", "cute-friendly-animals.json");

            if (!System.IO.File.Exists(resourcePath))
            {
                _logger.LogError("Animal resource file not found at: {Path}", resourcePath);
                return InternalServerErrorResponse("Không tìm thấy tệp dữ liệu động vật");
            }

            var jsonContent = await System.IO.File.ReadAllTextAsync(resourcePath);
            var animalData = JsonSerializer.Deserialize<AnimalListResponse>(jsonContent);

            if (animalData?.Animals == null || !animalData.Animals.Any())
            {
                _logger.LogError("Failed to parse animal data from JSON");
                return InternalServerErrorResponse("Không thể phân tích dữ liệu động vật");
            }

            return OkResponse(animalData.Animals, $"Lấy {animalData.Animals.Count} động vật thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving animals");
            return InternalServerErrorResponse($"Lỗi khi lấy danh sách động vật: {ex.Message}");
        }
    }

    // Helper class to deserialize JSON
    private class AnimalListResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("animals")]
        public List<string>? Animals { get; set; }
    }
}
