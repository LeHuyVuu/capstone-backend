using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Challenge
{
    public class UpdateChallengeRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string TriggerEvent { get; set; }
        public string GoalMetric { get; set; }
        public int TargetGoal { get; set; }
        public int RewardPoints { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; }

        public Dictionary<string, object> RuleData { get; set; }
    }
}
