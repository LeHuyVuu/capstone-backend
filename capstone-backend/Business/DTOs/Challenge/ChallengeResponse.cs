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

        public List<ChallengeRuleDisplayDto> Rules { get; set; } = new();
    }
    
    public class ChallengeRuleDisplayDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public object Operator { get; set; }

        public object RawValue { get; set; }
        public string DisplayValue { get; set; }
    }
}
