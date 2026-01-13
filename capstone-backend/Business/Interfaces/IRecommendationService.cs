using capstone_backend.Business.DTOs.Recommendation;

namespace capstone_backend.Business.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationResponse> GetRecommendationsAsync(string query, CancellationToken cancellationToken = default);
}
