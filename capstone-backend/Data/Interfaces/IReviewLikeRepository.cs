using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IReviewLikeRepository : IGenericRepository<ReviewLike>
    {
        Task<ReviewLike?> GetByReviewIdAndMemberIdAsync(int reviewId, int memberId);
    }
}
