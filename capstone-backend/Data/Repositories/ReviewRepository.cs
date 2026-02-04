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
            .Select(r => r.Rating.Value)
            .ToListAsync();
    }
}
