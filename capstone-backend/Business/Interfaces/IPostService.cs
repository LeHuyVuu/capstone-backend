using capstone_backend.Business.DTOs.Post;

namespace capstone_backend.Business.Interfaces
{
    public interface IPostService
    {
        Task<PostResponse> CreatePostAsync(int userId, CreatePostRequest request);
        Task<int> DeletePostAsync(int userId, int postId);
        Task<FeedResponse> GetFeedsAsync(int userId, FeedRequest request);
        Task<PostResponse> GetPostDetailsAsync(int userId, int postId);
        Task<PostLikeResponse> LikePostAsync(int userId, int postId);
        Task<PostResponse> UpdatePostAsync(int userId, int postId, UpdatePostRequest request);
    }
}
