using capstone_backend.Api.VenueRecommendation.Api.DTOs;

namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Meilisearch service - Indexing methods
/// </summary>
public partial class MeilisearchService
{
    /// <inheritdoc/>
    public async Task<bool> IndexVenueLocationAsync(int venueId)
    {
        try
        {
            var venue = await _venueLocationRepository.GetByIdWithDetailsAsync(venueId);

            if (venue == null || venue.IsDeleted == true)
            {
                _logger.LogWarning("Venue location {VenueId} not found or is deleted", venueId);
                return false;
            }

            var document = await MapToQueryResultAsync(venue);
            var index = _meilisearchClient.Index(_indexName);
            await index.AddDocumentsAsync(new[] { document });

            _logger.LogInformation("Indexed venue location {VenueId} to Meilisearch", venueId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing venue location {VenueId} to Meilisearch", venueId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> IndexAllVenueLocationsAsync()
    {
        try
        {
            var allVenues = await _venueLocationRepository.GetAllAsync();
            var venues = allVenues.Where(v => v.IsDeleted != true && v.Status == "ACTIVE").ToList();

            var documents = new List<VenueLocationQueryResult>();

            foreach (var venue in venues)
            {
                var venueWithDetails = await _venueLocationRepository.GetByIdWithDetailsAsync(venue.Id);
                if (venueWithDetails != null)
                {
                    var document = await MapToQueryResultAsync(venueWithDetails);
                    documents.Add(document);
                }
            }

            var index = _meilisearchClient.Index(_indexName);
            await index.AddDocumentsAsync(documents);

            _logger.LogInformation("Successfully indexed {Count} venue locations to Meilisearch", documents.Count);
            return documents.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing all venue locations to Meilisearch");
            return 0;
        }
    }
}
