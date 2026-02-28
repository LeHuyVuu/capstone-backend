namespace capstone_backend.Business.DTOs.Post
{
    public class CommentLikeResponse
    {
        public int CommentLikeCount { get; set; }
        public bool IsLikedByMe { get; set; }
    }
}
