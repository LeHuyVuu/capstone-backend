namespace capstone_backend.Business.DTOs.Challenge
{
    public class TodayMoodCheckinStatusResponse
    {
        public bool HasCheckedInToday { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }
}
