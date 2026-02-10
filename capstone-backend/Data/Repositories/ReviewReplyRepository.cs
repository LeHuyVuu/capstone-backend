using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class ReviewReplyRepository : GenericRepository<ReviewReply>, IReviewReplyRepository
    {
        public ReviewReplyRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<ReviewReply?> GetByReviewId(int reviewId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(rr => rr.ReviewId == reviewId);
        }
    }
}
