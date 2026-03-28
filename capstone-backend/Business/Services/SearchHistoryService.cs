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

    public async Task<PagedResult<SearchHistoryResponse>> GetSearchHistoriesByMemberAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (memberProfile == null)
        {
            _logger.LogWarning("No member profile found for user ID {UserId} when fetching search history", userId);
            return new PagedResult<SearchHistoryResponse>
            {
                Items = new List<SearchHistoryResponse>(),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }
        var memberId = memberProfile.Id;
        var query = _unitOfWork.Context.Set<SearchHistory>()
            .AsNoTracking()
            .Where(h => h.MemberId == memberId && h.IsDeleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(h => h.SearchedAt)
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
        var normalizedKeyword = NormalizeKeyword(keyword);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            _logger.LogDebug("Skip creating search history because keyword is empty for member {MemberId}", memberId);
            return new SearchHistoryResponse
            {
                Id = 0,
                MemberId = memberId,
                Keyword = string.Empty,
                ResultCount = resultCount,
                SearchedAt = DateTime.UtcNow
            };
        }

        var serializedFilterCriteria = filterCriteria != null ? JsonSerializer.Serialize(filterCriteria) : null;

        var existingHistory = await _unitOfWork.Context.Set<SearchHistory>()
            .AsNoTracking()
            .Where(h => h.MemberId == memberId && h.IsDeleted != true)
            .OrderByDescending(h => h.SearchedAt)
            .FirstOrDefaultAsync(h =>
                h.Keyword == normalizedKeyword &&
                h.FilterCriteria == serializedFilterCriteria,
                cancellationToken);

        if (existingHistory != null)
        {
            _logger.LogDebug(
                "Skipped duplicate search history for member {MemberId} - keyword: {Keyword}",
                memberId,
                normalizedKeyword);
            return MapToResponse(existingHistory);
        }

        var searchHistory = new SearchHistory
        {
            MemberId = memberId,
            Keyword = normalizedKeyword,
            FilterCriteria = serializedFilterCriteria,
            ResultCount = resultCount,
            SearchedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<SearchHistory>().AddAsync(searchHistory, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created search history {HistoryId} for member {MemberId} - keyword: {Keyword}", 
            searchHistory.Id, memberId, keyword);

        return MapToResponse(searchHistory);
    }

    public async Task<bool> DeleteSearchHistoryAsync(int id, int memberId, CancellationToken cancellationToken = default)
    {
        var searchHistory = await _unitOfWork.Context.Set<SearchHistory>()
            .FirstOrDefaultAsync(h => h.Id == id && h.MemberId == memberId && h.IsDeleted != true, cancellationToken);

        if (searchHistory == null)
            return false;

        searchHistory.IsDeleted = true;

        _unitOfWork.Context.Set<SearchHistory>().Update(searchHistory);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted search history {HistoryId}", id);

        return true;
    }

    public async Task<bool> ClearSearchHistoryAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var histories = await _unitOfWork.Context.Set<SearchHistory>()
            .Where(h => h.MemberId == memberId && h.IsDeleted != true)
            .ToListAsync(cancellationToken);

        foreach (var history in histories)
        {
            history.IsDeleted = true;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Cleared {Count} search histories for member {MemberId}", histories.Count, memberId);

        return true;
    }

    private SearchHistoryResponse MapToResponse(SearchHistory history)
    {
        object? filterCriteria = null;
        if (!string.IsNullOrEmpty(history.FilterCriteria))
        {
            try
            {
                filterCriteria = JsonSerializer.Deserialize<object>(history.FilterCriteria);
            }
            catch
            {
                filterCriteria = history.FilterCriteria;
            }
        }

        return new SearchHistoryResponse
        {
            Id = history.Id,
            MemberId = history.MemberId,
            Keyword = history.Keyword,
            ResultCount = history.ResultCount,
            SearchedAt = history.SearchedAt
        };
    }

    private static string NormalizeKeyword(string keyword)
    {
        return string.IsNullOrWhiteSpace(keyword) ? string.Empty : keyword.Trim();
    }
}
