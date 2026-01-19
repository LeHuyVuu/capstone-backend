using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SearchHistory;
using capstone_backend.Business.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services;

public class SearchHistoryService : ISearchHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SearchHistoryService> _logger;

    public SearchHistoryService(IUnitOfWork unitOfWork, ILogger<SearchHistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResult<SearchHistoryResponse>> GetSearchHistoriesByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<search_history>()
            .Where(h => h.member_id == memberId && h.is_deleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(h => h.searched_at)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SearchHistoryResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<SearchHistoryResponse> CreateSearchHistoryAsync(int? memberId, string keyword, object? filterCriteria, int resultCount, CancellationToken cancellationToken = default)
    {
        var searchHistory = new search_history
        {
            member_id = memberId,
            keyword = keyword,
            filter_criteria = filterCriteria != null ? JsonSerializer.Serialize(filterCriteria) : null,
            result_count = resultCount,
            searched_at = DateTime.UtcNow,
            is_deleted = false
        };

        await _unitOfWork.Context.Set<search_history>().AddAsync(searchHistory, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created search history {HistoryId} for member {MemberId} - keyword: {Keyword}", 
            searchHistory.id, memberId, keyword);

        return MapToResponse(searchHistory);
    }

    public async Task<bool> DeleteSearchHistoryAsync(int id, int memberId, CancellationToken cancellationToken = default)
    {
        var searchHistory = await _unitOfWork.Context.Set<search_history>()
            .FirstOrDefaultAsync(h => h.id == id && h.member_id == memberId && h.is_deleted != true, cancellationToken);

        if (searchHistory == null)
            return false;

        searchHistory.is_deleted = true;

        _unitOfWork.Context.Set<search_history>().Update(searchHistory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted search history {HistoryId}", id);

        return true;
    }

    public async Task<bool> ClearSearchHistoryAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var histories = await _unitOfWork.Context.Set<search_history>()
            .Where(h => h.member_id == memberId && h.is_deleted != true)
            .ToListAsync(cancellationToken);

        foreach (var history in histories)
        {
            history.is_deleted = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleared {Count} search histories for member {MemberId}", histories.Count, memberId);

        return true;
    }

    private SearchHistoryResponse MapToResponse(search_history history)
    {
        object? filterCriteria = null;
        if (!string.IsNullOrEmpty(history.filter_criteria))
        {
            try
            {
                filterCriteria = JsonSerializer.Deserialize<object>(history.filter_criteria);
            }
            catch
            {
                filterCriteria = history.filter_criteria;
            }
        }

        return new SearchHistoryResponse
        {
            Id = history.id,
            MemberId = history.member_id,
            Keyword = history.keyword,
            FilterCriteria = filterCriteria,
            ResultCount = history.result_count,
            SearchedAt = history.searched_at
        };
    }
}
