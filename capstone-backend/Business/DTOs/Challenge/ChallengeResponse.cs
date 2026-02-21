namespace capstone_backend.Business.DTOs.Challenge
{
    public class ChallengeResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? TriggerEvent { get; set; } = null!;
        public string? GoalMetric { get; set; } = null!;
        public int TargetGoal { get; set; } = 0;
        public int RewardPoints { get; set; } = 0;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = null!;

        public Dictionary<string, object> RuleData { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
    }
}
