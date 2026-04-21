using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Interest;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterestController : BaseController
{
    private readonly IInterestService _interestService;
    private readonly ILogger<InterestController> _logger;

    public InterestController(
        IInterestService interestService,
        ILogger<InterestController> logger)
    {
        _interestService = interestService;
        _logger = logger;
    }

    /// <summary>
    /// Get all interests/hobbies
    /// </summary>
    /// <param name="search">Optional search query to filter interests by name, nameEn, or category</param>
    /// <returns>List of interests</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InterestResponse>>), 200)]
    public async Task<IActionResult> GetInterests([FromQuery] string? search = null)
    {
        try
        {
            List<InterestResponse> interests;

            if (string.IsNullOrWhiteSpace(search))
            {
                interests = await _interestService.GetAllInterestsAsync();
                return OkResponse(interests, $"Đã lấy {interests.Count} sở thích");
            }
            else
            {
                interests = await _interestService.SearchInterestsAsync(search);
                return OkResponse(interests, $"Tìm thấy {interests.Count} sở thích phù hợp với '{search}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interests");
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy danh sách sở thích");
        }
    }

    /// <summary>
    /// Get interest categories
    /// </summary>
    /// <returns>List of unique categories</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var interests = await _interestService.GetAllInterestsAsync();
            var categories = interests
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return OkResponse(categories, $"Đã lấy {categories.Count} danh mục");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interest categories");
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy danh mục sở thích");
        }
    }
}
