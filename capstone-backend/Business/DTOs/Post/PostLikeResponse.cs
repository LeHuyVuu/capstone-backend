using Amazon.S3.Model;

namespace capstone_backend.Business.DTOs.Post
{
    public class PostLikeResponse
    {
        public int PostLikeCount { get; set; }
        public bool IsLikedByMe { get; set; }
    }
}
