using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SearchHistory;

namespace capstone_backend.Business.Interfaces;

public interface ISearchHistoryService
{
    Task<PagedResult<SearchHistoryResponse>> GetSearchHistoriesByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SearchHistoryResponse> CreateSearchHistoryAsync(int? memberId, string keyword, object? filterCriteria, int resultCount, CancellationToken cancellationToken = default);
    Task<bool> DeleteSearchHistoryAsync(int id, int memberId, CancellationToken cancellationToken = default);
    Task<bool> ClearSearchHistoryAsync(int memberId, CancellationToken cancellationToken = default);
}
