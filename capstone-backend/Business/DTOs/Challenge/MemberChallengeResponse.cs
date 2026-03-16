namespace capstone_backend.Business.DTOs.Challenge
{
    public class MemberChallengeResponse : ChallengeResponse
    {
        public bool IsJoined { get; set; }
        public int? CoupleChallengeId { get; set; }
        public string? CoupleChallengeStatus { get; set; }
        public int? CurrentProgress { get; set; }

        public bool IsRewardClaimed { get; set; } = false;
        public DateTime? RewardClaimedAt { get; set; }
        public int? RewardClaimedByMemberId { get; set; }
    }
}
