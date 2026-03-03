namespace capstone_backend.Business.DTOs.Challenge
{
    public class MemberChallengeDetailResponse : ChallengeResponse
    {
        public bool IsJoined { get; set; }
        public int? CoupleChallengeId { get; set; }
        public string? CoupleChallengeStatus { get; set; }
        public int? CurrentProgress { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}
