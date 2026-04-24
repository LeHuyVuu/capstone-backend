using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.DTOs.Review
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public int MemberId { get; set; }
        public int? CoupleProfileId { get; set; }
        public int? Rating { get; set; }
        public string? Content { get; set; }
        public DateTime? VisitedAt { get; set; }
        public bool? IsAnonymous { get; set; }
        public int? LikeCount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string>? ImageUrls { get; set; }
        public bool? IsMatched { get; set; }
        public bool? IsRelevant { get; set; }
        public string? CoupleMoodSnapshot { get; set; }

        public ReviewMemberInfo? Member { get; set; }
        public ReviewVenueInfo? Venue { get; set; }
    }

    public class ReviewVenueInfo
    {
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public List<string>? VenueCoverImage { get; set; }
    }
}
