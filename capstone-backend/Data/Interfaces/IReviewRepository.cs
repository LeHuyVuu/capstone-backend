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

    /// <summary>
    /// Lấy thông tin mood match statistics cho venue
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <returns>Tuple of total reviews count and matched reviews count</returns>
    Task<(int TotalCount, int MatchedCount)> GetMoodMatchStatisticsAsync(int venueId);

    /// <summary>
    /// Lấy danh sách reviews theo venueId kèm review likes, sắp xếp theo thời gian (có phân trang)
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortDescending">Sort by created time descending (default: true)</param>
    /// <returns>Tuple of reviews list with review likes and total count</returns>
    Task<(List<Review> Reviews, int TotalCount)> GetReviewsWithLikesByVenueIdAsync(int venueId, int page, int pageSize, bool sortDescending = true);

    /// <summary>
    /// Lấy danh sách reviews theo venueId kèm review likes, filter theo ngày/tháng/năm (có phân trang)
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="date">Specific date to filter</param>
    /// <param name="month">Month to filter (1-12)</param>
    /// <param name="year">Year to filter</param>
    /// <param name="sortDescending">Sort by created time descending (default: true)</param>
    /// <returns>Tuple of reviews list with review likes and total count</returns>
    Task<(List<Review> Reviews, int TotalCount)> GetReviewsByDateFilterAsync(
        int venueId, 
        int page, 
        int pageSize, 
        DateTime? date = null,
        int? month = null, 
        int? year = null,
        bool sortDescending = true);
    Task<bool> HasMemberReviewedVenueAsync(int memberId, int venueId);
}
