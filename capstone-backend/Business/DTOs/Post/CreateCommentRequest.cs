namespace capstone_backend.Business.DTOs.Post
{
    public class CreateCommentRequest
    {
        /// <example>Đây là một bình luận</example>
        public string Content { get; set; }

        /// <example>null</example>
        public int? ParentId { get; set; } = null;
    }
}
