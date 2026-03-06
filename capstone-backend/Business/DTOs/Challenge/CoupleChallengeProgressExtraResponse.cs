namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeProgressExtraResponse
    {
        public string Type { get; set; } = default!; // CHECKIN / REVIEW / POST /
    }

    /// <summary>
    /// <para>Date</para>
    /// <para>CanCheckinToday</para>
    /// <para>DoneMembersToday</para>
    /// <para>TotalMembers</para>
    /// <para>CurrentStreak</para>
    /// <para>LongestStreak</para>
    /// </summary>
    public class CheckinChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public DateOnly Date { get; set; }
        public bool CanCheckinToday { get; set; }
        public int DoneMembersToday { get; set; }
        public int TotalMembers { get; set; }

        public int MemberCurrentStreak { get; set; }
        public int MemberLongestStreak { get; set; }

        public int CoupleCurrentStreak { get; set; }
        public int CoupleLongestStreak { get; set; }
    }

    /// <summary>
    /// <para>QualifiedReviewCount</para>
    /// <para>LastQualifiedReviewAt</para>
    /// <para>LastVenueId</para>
    /// <para>LastVenueName</para>
    /// </summary>
    public class ReviewChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public int QualifiedReviewCount { get; set; }
        public DateTime? LastQualifiedReviewAt { get; set; }

        public int? LastVenueId { get; set; }
        public string? LastVenueName { get; set; }
    }

    /// <summary>
    /// <para>QualifiedPostCount</para>
    /// <para>LastQualifiedPostAt</para>
    /// <para>LastPostId</para>
    /// </summary>
    public class PostChallengeProgressExtraResponse : CoupleChallengeProgressExtraResponse
    {
        public int QualifiedPostCount { get; set; }
        public DateTime? LastQualifiedPostAt { get; set; }

        public int? LastPostId { get; set; }
    }
}
