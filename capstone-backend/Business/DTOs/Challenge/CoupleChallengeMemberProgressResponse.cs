namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeMemberProgressResponse
    {
        public int MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AvatarUrl { get; set; }

        public bool IsCurrentUser { get; set; }

        public bool IsJoined { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }

        public bool? HasDoneToday { get; set; }
        public DateTime? LastActionAt { get; set; }
        public int? ContributionCount { get; set; }
    }
}
