namespace capstone_backend.Business.DTOs.SearchHistory;

public class SearchHistoryResponse
{
    public int Id { get; set; }
    public int? MemberId { get; set; }
    public string Keyword { get; set; } = null!;
    public object? FilterCriteria { get; set; } // JSONB
    public int? ResultCount { get; set; }
    public DateTime? SearchedAt { get; set; }
}
