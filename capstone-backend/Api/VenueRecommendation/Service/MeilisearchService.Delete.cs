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

    /// <inheritdoc/>
    public async Task<bool> ClearIndexV2Async()
    {
        try
        {
            var v2Host = Environment.GetEnvironmentVariable("MEILISEARCH_V2_HOST")
                         ?? "http://134.209.108.208:7700";

            var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                         ?? "couplemood123";

            var v2Client = new Meilisearch.MeilisearchClient(v2Host, apiKey);
            var index = v2Client.Index(_indexName);

            await index.DeleteAllDocumentsAsync();

            _logger.LogInformation("Cleared Meilisearch v2 index {Index} on host {Host}", _indexName, v2Host);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing Meilisearch v2 index");
            return false;
        }
    }
}
