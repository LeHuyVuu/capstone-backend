using capstone_backend.Data.Entities;

namespace capstone_backend.Business.DTOs.Post
{
    public class PostResponse
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<MediaItem>? MediaPayload { get; set; }
        public string LocationName { get; set; }
        public List<string> HashTags { get; set; }
        public List<string> Topic { get; set; }

        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public int AuthorId { get; set; }
        public bool IsLikedByMe { get; set; }
        public bool IsOwner { get; set; }

        public AuthorResponse Author { get; set; }
    }
}
