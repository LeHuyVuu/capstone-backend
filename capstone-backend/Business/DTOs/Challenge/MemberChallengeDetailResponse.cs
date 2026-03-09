namespace capstone_backend.Business.DTOs.Challenge
{
    public class MemberChallengeDetailResponse : ChallengeResponse
    {
        public bool IsJoined { get; set; } = false;
        public int? CoupleChallengeId { get; set; } = null;
        public string? CoupleChallengeStatus { get; set; } = null;
        public int? CurrentProgress { get; set; } = null;
        public DateTime? JoinedAt { get; set; } = null;
    }
}
