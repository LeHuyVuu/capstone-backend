using capstone_backend.Data.Entities;

namespace capstone_backend.Business.DTOs.Post
{
    public class PostFeedResponse : PostResponse
    {
        public double TotalScore { get; set; }
    }
}
