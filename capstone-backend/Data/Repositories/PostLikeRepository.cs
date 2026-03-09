using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class PostLikeRepository : GenericRepository<PostLike>, IPostLikeRepository
    {
        public PostLikeRepository(MyDbContext context) : base(context)
        {
        }
    }
}
