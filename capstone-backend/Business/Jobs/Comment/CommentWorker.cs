
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Jobs.Comment
{
    public class CommentWorker : ICommentWorker
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommentWorker(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RecountPostAsync(int postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted == true)
                return;

            post.CommentCount = await _unitOfWork.Comments.CountAsync(
                c => c.PostId == postId && 
                c.IsDeleted == false &&
                c.Status == CommentStatus.PUBLISHED.ToString()
            );

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecountReplyAsync(int commentId)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                return;

            comment.ReplyCount = await _unitOfWork.Comments.CountAsync(
                c => c.ParentId == commentId && 
                c.IsDeleted == false &&
                c.Status == CommentStatus.PUBLISHED.ToString()
            );

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
