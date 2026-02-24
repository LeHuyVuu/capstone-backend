namespace capstone_backend.Business.DTOs.Post
{
    public class FeedResponse
    {
        public List<PostResponse> Posts { get; set; } = new();
        public long? NextCursor { get; set; }
        public bool HasMore { get; set; }
    }
}
