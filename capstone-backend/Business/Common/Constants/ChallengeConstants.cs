using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Common.Constants
{
    public static class ChallengeConstants
    {
        public static class GoalMetrics
        {
            public const string COUNT = "COUNT";
            public const string UNIQUE_LIST = "UNIQUE_LIST";
            public const string Streak = "STREAK";
        }

        public static class RuleKeys
        {
            public const string VENUE_ID = "venue_id";
            public const string HASH_TAG = "hashtags";
            public const string HAS_IMAGE = "has_image";
        }

        public static class RuleOps
        {
            public const string Eq = "EQ";
            public const string In = "IN";
            public const string Contains = "CONTAINS";
        }

        public static List<string> AllowedTriggerEvents = new List<string>
        {
            ChallengeTriggerEvent.CHECKIN.ToString(),
            ChallengeTriggerEvent.REVIEW.ToString(),
            ChallengeTriggerEvent.POST.ToString()
        };

        public static List<string> AllowedGoalMetrics = new List<string>
        {
            GoalMetrics.COUNT,
            GoalMetrics.UNIQUE_LIST,
            GoalMetrics.Streak
        };
    }
}
