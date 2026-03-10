namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeDetailResponse
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public int CoupleId { get; set; }
        public string Status { get; set; } = default!;

        public int CurrentProgress { get; set; }
        public int TargetProgress { get; set; }
        public int RemainingProgress => Math.Max(TargetProgress - CurrentProgress, 0);

        public bool IsJoined { get; set; }
        public DateTime? JoinedAt { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        public bool IsRewardClaimed { get; set; } = false;
        public DateTime? RewardClaimedAt { get; set; }
        public int? RewardClaimedByMemberId { get; set; }

        public string? ProgressText { get; set; }
        public object? ProgressExtra { get; set; } 

        public List<CoupleChallengeMemberProgressResponse> Members { get; set; } = new();

        public ChallengeResponse Challenge { get; set; } = default!;
    }
}
