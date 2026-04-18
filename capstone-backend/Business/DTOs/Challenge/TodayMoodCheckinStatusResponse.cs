namespace capstone_backend.Business.DTOs.Challenge
{
    public class TodayMoodCheckinStatusResponse
    {
        public bool HasCheckedInToday { get; set; }
        public DateTime? CheckedInAt { get; set; }

        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        public DateOnly StartDay { get; set; }
        public DateOnly EndDay { get; set; }

        public List<MoodCheckinCalendarDayItemResponse> Days { get; set; } = new();
    }

    public class MoodCheckinCalendarDayItemResponse
    {
        public DateOnly Date { get; set; }
        public bool HasCheckedIn { get; set; }
    }
}
