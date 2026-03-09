namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeListItemResponse
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public int CoupleId { get; set; }
        public string Status { get; set; } = default!;

        public string? ProgressText { get; set; }
        public int CurrentProgress { get; set; }
        public int TargetProgress { get; set; }
        public int RemainingProgress => Math.Max(TargetProgress - CurrentProgress, 0);
        public bool IsCompleted { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Challenge snapshot
        public ChallengeResponse Challenge { get; set; }
    }
}
