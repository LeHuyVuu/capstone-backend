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
            public const string Contains = "CONTAINS";
        }
    }
}
