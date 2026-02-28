namespace capstone_backend.Business.DTOs.Post
{
    public class CommentResponse
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Content { get; set; }
        public int AuthorId { get; set; }
        public AuthorResponse Author { get; set; }
        public DateTime CreatedAt { get; set; }

        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }

        public int RootId { get; set; }
        public int Level { get; set; }
    }
}
