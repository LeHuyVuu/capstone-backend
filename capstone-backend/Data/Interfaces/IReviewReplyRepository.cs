using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IReviewReplyRepository : IGenericRepository<ReviewReply>
    {
        Task<ReviewReply?> GetByReviewId(int reviewId);
    }
}
