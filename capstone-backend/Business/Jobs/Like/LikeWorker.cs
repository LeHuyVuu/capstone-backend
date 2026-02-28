
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Jobs.Like
{
    public class LikeWorker : ILikeWorker
    {
        private readonly IUnitOfWork _unitOfWork;

        public LikeWorker(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RecountPostLikeAsync(int postId)
        {
            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null)
                return;

            post.LikeCount = post.PostLikes.Count(pl => pl.PostId == postId);

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
