using capstone_backend.Business.DTOs.Post;

namespace capstone_backend.Business.Interfaces
{
    public interface IPostService
    {
        Task<FeedResponse> GetFeedsAsync(int value, FeedRequest request);
        Task<PostResponse> GetPostDetailsAsync(int userId, int postId);
    }
}
