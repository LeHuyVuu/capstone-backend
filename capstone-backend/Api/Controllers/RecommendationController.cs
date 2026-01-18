using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecommendationController : BaseController
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationController> _logger;

    public RecommendationController(
        IRecommendationService recommendationService,
        ILogger<RecommendationController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy gợi ý dựa trên OpenAI Assistant
    /// </summary>
    /// <param name="request">Câu hỏi/yêu cầu từ người dùng</param>
    /// <param name="cancellationToken">Token để hủy request</param>
    /// <returns>Danh sách các gợi ý</returns>
    [HttpPost]
    public async Task<IActionResult> GetRecommendations(
        [FromBody] RecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequestResponse("Query không được để trống");
        }

        _logger.LogInformation("Processing recommendation request: {Query}", request.Query);

        var result = await _recommendationService.GetRecommendationsAsync(
            request.Query, 
            cancellationToken);

        return OkResponse(result, "Đã lấy gợi ý thành công");
    }
}
