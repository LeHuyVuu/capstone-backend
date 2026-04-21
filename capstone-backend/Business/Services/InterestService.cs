using System.Text.Json;
using capstone_backend.Business.DTOs.Interest;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public interface IInterestService
{
    Task<List<InterestResponse>> GetAllInterestsAsync();
    Task<List<InterestResponse>> SearchInterestsAsync(string query);
}

public class InterestService : IInterestService
{
    private readonly ILogger<InterestService> _logger;
    private readonly string _jsonFilePath;
    private List<InterestResponse>? _cachedInterests;

    public InterestService(ILogger<InterestService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _jsonFilePath = Path.Combine(env.ContentRootPath, "Data", "SeedData", "interests.json");
    }

    public async Task<List<InterestResponse>> GetAllInterestsAsync()
    {
        if (_cachedInterests != null)
        {
            _logger.LogInformation("Returning cached interests");
            return _cachedInterests;
        }

        _logger.LogInformation("Loading interests from JSON file: {FilePath}", _jsonFilePath);

        if (!File.Exists(_jsonFilePath))
        {
            _logger.LogWarning("Interests JSON file not found at {FilePath}", _jsonFilePath);
            return new List<InterestResponse>();
        }

        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var data = JsonSerializer.Deserialize<InterestData>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _cachedInterests = data?.Interests ?? new List<InterestResponse>();
        _logger.LogInformation("Loaded {Count} interests from JSON", _cachedInterests.Count);

        return _cachedInterests;
    }

    public async Task<List<InterestResponse>> SearchInterestsAsync(string query)
    {
        var allInterests = await GetAllInterestsAsync();

        if (string.IsNullOrWhiteSpace(query))
        {
            return allInterests;
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        var results = allInterests.Where(i =>
            i.Name.ToLowerInvariant().Contains(normalizedQuery) ||
            i.NameEn.ToLowerInvariant().Contains(normalizedQuery) ||
            i.Category.ToLowerInvariant().Contains(normalizedQuery)
        ).ToList();

        _logger.LogInformation("Search query '{Query}' returned {Count} results", query, results.Count);

        return results;
    }

    private class InterestData
    {
        public List<InterestResponse> Interests { get; set; } = new();
    }
}
