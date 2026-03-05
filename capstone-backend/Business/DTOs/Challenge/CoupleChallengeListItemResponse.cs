namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeListItemResponse
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public int CoupleId { get; set; }

        public string Status { get; set; } = default!;
        public int CurrentProgress { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Challenge snapshot
        public ChallengeResponse Challenge { get; set; }
    }
}
