namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeProgressExtraResponse
    {
        public string Type { get; set; } = default!; // CHECKIN / REVIEW / POST /
    }

    public class CheckinChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public DateOnly Date { get; set; }
        public bool CanCheckinToday { get; set; }
        public int DoneMembersToday { get; set; }
        public int TotalMembers { get; set; }

        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
    }

    public class ReviewChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public int QualifiedReviewCount { get; set; }
        public DateTime? LastQualifiedReviewAt { get; set; }

        public int? LastVenueId { get; set; }
        public string? LastVenueName { get; set; }
    }

    public class PostChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public int QualifiedPostCount { get; set; }
        public DateTime? LastQualifiedPostAt { get; set; }

        public int? LastPostId { get; set; }
    }
}
