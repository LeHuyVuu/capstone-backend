namespace capstone_backend.Business.DTOs.Post
{
    public class CommentResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int AuthorId { get; set; }
        public AuthorResponse Author { get; set; }
        public DateTime CreatedAt { get; set; }

        public int PostCommentCount { get; set; }
    }
}
