namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeProgressData
    {
        /// <summary>
        /// Version of challenge, use later if structure of progress data change
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// CHECKIN | REVIEW | POST
        /// </summary>
        public string Trigger { get; set; } = "";

        /// <summary>
        /// COUNT | UNIQUE_LIST | STREAK
        /// </summary>
        public string Metric { get; set; } = "";

        /// <summary>
        /// Challenge's objective
        /// </summary>
        public int Target { get; set; } = 0;

        /// <summary>
        /// How many target is completed
        /// </summary>
        public int Current { get; set; } = 0;

        public bool IsCompleted { get; set; } = false;

        public Dictionary<string, ProgressMember> Members { get; set; } = new();

        public ProgressUnique Unique { get; set; } = new();

        public ProgressStreak Streak { get; set; } = new();

        public List<ProgressEvent>? Events { get; set; } = null;

        public DailyHistory? DailyHistory { get; set; } = null;
    }

    public class ProgressMember
    {
        public int Current { get; set; } = 0;
        public int Streak { get; set; } = 0;
        public DateTime? LastActionAt { get; set; } = null;
    }

    public class ProgressUnique
    {
        public List<string> Items { get; set; } = new();
        public Dictionary<string, List<string>> ByMember { get; set; } = new();
    }

    public class ProgressStreak
    {
        public string Mode { get; set; } = ""; // DAILY | WEEKLY | MONTHLY
        public int Current { get; set; } = 0;
        public int Best { get; set; } = 0;
        public DateTime? LastActionAt { get; set; } = null;
        public Dictionary<string, StreakByMember> ByMember { get; set; } = new();
    }

    public class StreakByMember
    {
        public int Current { get; set; } = 0;
        public int Best { get; set; } = 0;
        public DateTime? LastAt { get; set; } = null;
    }

    public class ProgressEvent
    {
        public DateTime At { get; set; }
        public string Type { get; set; } = ""; // CHECKIN | REVIEW | POST
        public int ActorMemberId { get; set; }
        public string RefId { get; set; } = "";
        public int VenueId { get; set; }
        public List<string> HashTags { get; set; } = new();
        public bool HasImage { get; set; } = false;
        public string UniqueKey { get; set; } = "";
    }

    public class DailyHistory
    {
        public string Tz { get; set; } = "Asia/Ho_Chi_Minh";
        public Dictionary<string, Dictionary<string, int>> Months { get; set; } = new();
    }
}
