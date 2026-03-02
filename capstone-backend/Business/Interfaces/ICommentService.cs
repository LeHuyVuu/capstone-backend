using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Post;

namespace capstone_backend.Business.Interfaces
{
    public interface ICommentService
    {
        Task<CommentResponse> CommentPostAsync(int userId, int postId, CreateCommentRequest request);
        Task<CommentResponse> UpdateCommentAsync(int userId, int commentId, UpdateCommentRequest request);
        Task<int> DeleteCommentAsync(int userId, int commentId);
        Task<PagedResult<CommentResponse>> GetRepliesAsync(int commentId, int pageNumber, int pageSize);
        Task<CommentLikeResponse> LikeCommentAsync(int userId, int commentId);
        Task<CommentLikeResponse> UnlikeCommentAsync(int userId, int commentId);
        Task<CommentResponse> GetCommentByIdAsync(int commentId);
    }
}
