namespace capstone_backend.Business.DTOs.Challenge
{
    public class MemberChallengeResponse : ChallengeResponse
    {
        public bool IsJoined { get; set; }
        public int? CoupleChallengeId { get; set; }
        public string? CoupleChallengeStatus { get; set; }
        public int? CurrentProgress { get; set; }
    }
}
