namespace capstone_backend.Business.DTOs.Review
{
    public class MyReviewResponse
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public string? VenueCoverImage { get; set; }

        public int? Rating { get; set; }
        public string? Content { get; set; }
        public DateTime? VisitedAt { get; set; }

        public bool? IsAnonymous { get; set; }
        public bool? IsMatched { get; set; }

        public int LikeCount { get; set; }
        public string? Status { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public bool HasReply { get; set; }
        public MyReviewReplyInfo? Reply { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class MyReviewReplyInfo
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
