namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeListItemResponse
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }

        public string Status { get; set; } = default!;
        public int CurrentProgress { get; set; }
        public DateTime? JoinedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Challenge snapshot
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int RewardPoints { get; set; }
        public string? GoalMetric { get; set; }
        public int TargetGoal { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
