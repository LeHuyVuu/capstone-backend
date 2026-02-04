using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

/// <summary>
/// Repository cho Review entity
/// </summary>
public interface IReviewRepository : IGenericRepository<Review>
{
    /// <summary>
    /// Lấy danh sách reviews theo venueId (có phân trang)
    /// </summary>
    Task<(List<Review> Reviews, int TotalCount)> GetReviewsByVenueIdAsync(int venueId, int page, int pageSize);

    /// <summary>
    /// Lấy tất cả ratings cho venue (để tính summary)
    /// </summary>
    Task<List<int>> GetAllRatingsByVenueIdAsync(int venueId);
}
