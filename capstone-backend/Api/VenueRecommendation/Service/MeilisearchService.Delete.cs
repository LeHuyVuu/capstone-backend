namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Meilisearch service - Delete methods
/// </summary>
public partial class MeilisearchService
{
    /// <inheritdoc/>
    public async Task<bool> DeleteVenueLocationAsync(int venueId)
    {
        try
        {
            var index = _meilisearchClient.Index(_indexName);
            await index.DeleteOneDocumentAsync(venueId.ToString());

            _logger.LogInformation("Deleted venue location {VenueId} from Meilisearch", venueId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting venue location {VenueId} from Meilisearch", venueId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ClearIndexAsync()
    {
        try
        {
            var index = _meilisearchClient.Index(_indexName);
            
            await index.DeleteAllDocumentsAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing Meilisearch index");
            return false;
        }
    }
}
