using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<Post?> GetByShareCodeAsync(string shareCode)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Author)
                    .ThenInclude(a => a.User)
                .Include(p => p.PostLikes)
                .FirstOrDefaultAsync(p => p.ShareCode == shareCode && p.IsDeleted == false);
        }

        public async Task<IEnumerable<Post>> GetPostsByMemberId(int memberId, int pageSize = 20, long? cursor = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.Author)
                    .ThenInclude(a => a.User)
                .Include(p => p.PostLikes)
                .Where(p => p.Visibility == "PUBLIC" && p.Status == "PUBLISHED" && p.IsDeleted == false);

            if (cursor.HasValue)
                query = query.Where(p => p.Id < cursor.Value);

            return await query
                .OrderByDescending(p => p.Id)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Post?> GetPostWithIncludeById(int postId)
        {
            return await _dbSet
                .Include(p => p.Author)
                    .ThenInclude(a => a.User)
                .Include(p => p.PostLikes)
                .FirstOrDefaultAsync(p => p.Id == postId && p.IsDeleted == false);
        }
    }
}
