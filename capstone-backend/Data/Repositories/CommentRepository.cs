using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<Comment?> GetByIdIncludeAsync(int commentId)
        {
            return await _dbSet
                .Include(c => c.Post)
                .Include(c => c.Author)
                .Include(c => c.TargetMember)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.IsDeleted == false && c.Status == CommentStatus.PUBLISHED.ToString());
        }

        public async Task<Comment?> GetByIdIncludeWithAllStatusAsync(int commentId)
        {
            return await _dbSet
                .Include(c => c.Post)
                    .ThenInclude(p => p.Author)
                .Include(c => c.Author)
                .Include(c => c.TargetMember)
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.IsDeleted == false);
        }

        public async Task<IEnumerable<Comment>> GetChildCommentsByParentIdAsync(int parentId)
        {
            return await _dbSet
                .Where(c => c.ParentId == parentId && c.IsDeleted == false)
                .ToListAsync();
        }
    }
}
