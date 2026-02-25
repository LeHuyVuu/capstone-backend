using capstone_backend.Business.DTOs.Post;

namespace capstone_backend.Business.Interfaces
{
    public interface IPostService
    {
        Task<FeedResponse> GetFeedsAsync(int value, FeedRequest request);
        Task<FeedResponse> GetPostDetailsAsync(int postId);
    }
}
