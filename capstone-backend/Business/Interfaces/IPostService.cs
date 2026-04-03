using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Post;

namespace capstone_backend.Business.Interfaces
{
    public interface IPostService
    {
        
        Task<PostResponse> CreatePostAsync(int userId, CreatePostRequest request);
        Task<int> DeletePostAsync(int userId, int postId);
        Task<PagedResult<CommentResponse>> GetCommentsPostAsync(int userId, int postId, int pageNumber = 1, int pageSize = 10);
        Task<FeedResponse> GetFeedsAsync(int userId, FeedRequest request);
        Task<PagedResult<PostResponse>> GetFlaggedPostsAsync(int pageNumber, int pageSize);
        Task<ShareLinkResponse> GetLinkAsync(int postId);
        Task<PostResponse> GetPostDetailsAnonymousAsync(int postId);
        Task<PostResponse> GetPostDetailsAsync(int userId, int postId);
        Task<PostResponse> GetPostDetailsByShareLinkAsync(string shareCode);
        Task<PagedResult<PostResponse>> GetPostsMemberProfileAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<PostResponse>> GetPostsOtherProfileAsync(int userId, int memberId, int pageNumber, int pageSize);
        Task<PostLikeResponse> LikePostAsync(int userId, int postId);
        Task<int> ModerateFlaggedPostAsync(int postId, ModerationRequest request);
        Task<PostLikeResponse> UnlikePostAsync(int userId, int postId);
        Task<PostResponse> UpdatePostAsync(int userId, int postId, UpdatePostRequest request);
    }
}
