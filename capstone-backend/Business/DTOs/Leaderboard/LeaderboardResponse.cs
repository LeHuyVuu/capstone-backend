namespace capstone_backend.Business.DTOs.Leaderboard;

public class LeaderboardResponse
{
    public int Id { get; set; }
    public int CoupleId { get; set; }
    public string? CoupleName { get; set; }
    public int? TotalPoints { get; set; }
    public int? RankPosition { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class LeaderboardListResponse
{
    public string? PeriodType { get; set; }
    public string? SeasonKey { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public List<LeaderboardResponse> Rankings { get; set; } = new();
    public int TotalCount { get; set; }
}
