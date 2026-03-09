namespace capstone_backend.Business.DTOs.Post
{
    public class CommentResponse
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Content { get; set; }
        public int AuthorId { get; set; }
        public MemberCommentResponse Author { get; set; }
        public MemberCommentResponse ReplyToMember { get; set; }
        public DateTime CreatedAt { get; set; }

        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }

        public bool IsLikedByMe { get; set; }
        public bool IsOwner { get; set; }

        public int RootId { get; set; }
        public int Level { get; set; }
    }
}
