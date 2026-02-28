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

        public async Task<IEnumerable<Comment>> GetChildCommentsByParentIdAsync(int parentId)
        {
            return await _dbSet
                .Where(c => c.ParentId == parentId && c.IsDeleted == false)
                .ToListAsync();
        }
    }
}
