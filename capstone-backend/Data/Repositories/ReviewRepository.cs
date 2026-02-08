using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository cho Review entity
/// </summary>
public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(MyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Lấy danh sách reviews theo venueId (có phân trang)
    /// </summary>
    public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsByVenueIdAsync(int venueId, int page, int pageSize)
    {
        var query = _context.Set<Review>()
            .Include(r => r.Member)
                .ThenInclude(m => m!.User)
            .Where(r => r.VenueId == venueId && r.IsDeleted != true)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <summary>
    /// Lấy tất cả ratings cho venue (để tính summary)
    /// </summary>
    public async Task<List<int>> GetAllRatingsByVenueIdAsync(int venueId)
    {
        return await _context.Set<Review>()
            .Where(r => r.VenueId == venueId && r.IsDeleted != true && r.Rating.HasValue)
            .Select(r => r.Rating!.Value)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy thông tin mood match statistics cho venue
    /// </summary>
    public async Task<(int TotalCount, int MatchedCount)> GetMoodMatchStatisticsAsync(int venueId)
    {
        var query = _context.Set<Review>()
            .Where(r => r.VenueId == venueId && r.IsDeleted != true);

        var totalCount = await query.CountAsync();
        var matchedCount = await query.CountAsync(r => r.IsMatched == true);

        return (totalCount, matchedCount);
    }

    /// <summary>
    /// Lấy danh sách reviews theo venueId kèm review likes, sắp xếp theo thời gian (có phân trang)
    /// </summary>
    public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsWithLikesByVenueIdAsync(
        int venueId, 
        int page, 
        int pageSize, 
        bool sortDescending = true)
    {
        var query = _context.Set<Review>()
            .Include(r => r.Member)
                .ThenInclude(m => m!.User)
            .Include(r => r.ReviewLikes)
                .ThenInclude(rl => rl.Member)
                    .ThenInclude(m => m!.User)
            .Where(r => r.VenueId == venueId && r.IsDeleted != true);

        // Sắp xếp theo thời gian
        query = sortDescending 
            ? query.OrderByDescending(r => r.CreatedAt)
            : query.OrderBy(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery() // Tối ưu performance khi có nhiều Include
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <summary>
    /// Lấy danh sách reviews theo venueId kèm review likes, filter theo ngày/tháng/năm (có phân trang)
    /// </summary>
    public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsByDateFilterAsync(
        int venueId,
        int page,
        int pageSize,
        DateTime? date = null,
        int? month = null,
        int? year = null,
        bool sortDescending = true)
    {
        var query = _context.Set<Review>()
            .Include(r => r.Member)
                .ThenInclude(m => m!.User)
            .Include(r => r.ReviewLikes)
                .ThenInclude(rl => rl.Member)
                    .ThenInclude(m => m!.User)
            .Where(r => r.VenueId == venueId && r.IsDeleted != true);

        // Filter theo date (ưu tiên cao nhất)
        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Date == targetDate);
        }
        // Filter theo month và year
        else if (month.HasValue && year.HasValue)
        {
            query = query.Where(r => r.CreatedAt.HasValue 
                && r.CreatedAt.Value.Month == month.Value 
                && r.CreatedAt.Value.Year == year.Value);
        }
        // Filter chỉ theo year
        else if (year.HasValue)
        {
            query = query.Where(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Year == year.Value);
        }

        // Sắp xếp theo thời gian
        query = sortDescending
            ? query.OrderByDescending(r => r.CreatedAt)
            : query.OrderBy(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return (reviews, totalCount);
    }
}
