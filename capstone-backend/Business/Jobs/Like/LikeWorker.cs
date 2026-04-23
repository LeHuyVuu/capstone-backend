
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.Like
{
    public class LikeWorker : ILikeWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LikeWorker> _logger;

        public LikeWorker(IUnitOfWork unitOfWork, ILogger<LikeWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecountCommentLikeAsync(int commentId)
        {
            var comment = await _unitOfWork.Comments.GetByIdIncludeAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                return;

            comment.LikeCount = comment.CommentLikes.Count(cl => cl.CommentId == commentId);

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.SaveChangesAsync();
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
