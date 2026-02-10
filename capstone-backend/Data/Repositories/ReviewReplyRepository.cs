using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class ReviewReplyRepository : GenericRepository<ReviewReply>, IReviewReplyRepository
    {
        public ReviewReplyRepository(MyDbContext context) : base(context)
        {
        }
    }
}
