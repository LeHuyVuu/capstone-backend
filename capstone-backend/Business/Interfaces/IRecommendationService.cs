using capstone_backend.Business.DTOs.Recommendation;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Interface for AI-powered venue recommendation service
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets AI-powered venue recommendations
    /// </summary>
    Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request);
}
