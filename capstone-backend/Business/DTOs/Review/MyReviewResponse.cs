using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.DTOs.Review
{
    public class MyReviewResponse
    {

        public bool IsOwner { get; set; }
        public bool IsLikedByMe { get; set; }
        public int Id { get; set; }

        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public List<string>? VenueCoverImage { get; set; }

        public int? Rating { get; set; }
        public string? Content { get; set; }
        public DateTime? VisitedAt { get; set; }

        public bool? IsAnonymous { get; set; }
        public bool? IsMatched { get; set; }
        public bool? IsRelevant { get; set; }

        public int LikeCount { get; set; }
        public string? Status { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public bool HasReply { get; set; }
        public ReviewReplyResponse? ReviewReply { get; set; }

        public ReviewMemberInfo? Member { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
