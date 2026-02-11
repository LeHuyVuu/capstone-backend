using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class ReviewLikeRepository : GenericRepository<ReviewLike>, IReviewLikeRepository
    {
        public ReviewLikeRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<ReviewLike?> GetByReviewIdAndMemberIdAsync(int reviewId, int memberId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(rl => rl.ReviewId == reviewId && rl.MemberId == memberId);
        }
    }
}
