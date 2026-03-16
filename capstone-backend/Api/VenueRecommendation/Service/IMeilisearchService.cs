using capstone_backend.Api.VenueRecommendation.Api.DTOs;

namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Service interface for Meilisearch venue location query operations
/// </summary>
public interface IMeilisearchService
{
    /// <summary>
    /// Index a single venue location to Meilisearch
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <returns>True if successful</returns>
    Task<bool> IndexVenueLocationAsync(int venueId);

    /// <summary>
    /// Index all venue locations to Meilisearch
    /// </summary>
    /// <returns>Number of venues indexed</returns>
    Task<int> IndexAllVenueLocationsAsync();

    /// <summary>
    /// Query venue locations with filters
    /// </summary>
    /// <param name="request">Query request with filters</param>
    /// <param name="coupleMoodTypeName">Couple mood type name for filtering</param>
    /// <param name="memberMoodTypeName">Member mood type name for filtering</param>
    /// <param name="couplePersonalityTypeName">Couple personality type name for filtering</param>
    /// <param name="memberMbtiType">Member MBTI type for filtering</param>
    /// <param name="memberId">Optional member ID for saving search history</param>
    /// <returns>Query response with paginated results</returns>
    Task<VenueLocationQueryResponse> SearchVenueLocationsAsync(VenueLocationQueryRequest request, string? coupleMoodTypeName, string? memberMoodTypeName, string? couplePersonalityTypeName, string? memberMbtiType, int? memberId = null);

    /// <summary>
    /// Delete a venue location from Meilisearch index
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteVenueLocationAsync(int venueId);

    /// <summary>
    /// Clear all documents from Meilisearch index
    /// </summary>
    /// <returns>True if successful</returns>
    Task<bool> ClearIndexAsync();

    /// <summary>
    /// Configure index settings (searchable, filterable, sortable attributes)
    /// </summary>
    /// <returns>True if successful</returns>
    Task<bool> ConfigureIndexSettingsAsync();

    /// <summary>
    /// Verify index settings are correctly configured
    /// </summary>
    /// <returns>Settings verification result</returns>
    Task<object> VerifyIndexSettingsAsync();
}
