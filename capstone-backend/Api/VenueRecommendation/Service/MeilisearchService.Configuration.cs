namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Meilisearch service - Configuration methods
/// </summary>
public partial class MeilisearchService
{
    /// <inheritdoc/>
    public async Task<bool> ConfigureIndexSettingsAsync()
    {
        try
        {
            var index = _meilisearchClient.Index(_indexName);

            _logger.LogInformation("Configuring Meilisearch index settings...");

            await index.UpdateSearchableAttributesAsync(new[]
            {
                "name", "description", "address", "category", "area",
                "coupleMoodTypeNames", "couplePersonalityTypeNames", "venueOwnerName"
            });
            _logger.LogInformation("Updated searchable attributes");

            await index.UpdateFilterableAttributesAsync(new[]
            {
                "id", "category", "area", "averageRating", "priceMin", "priceMax",
                "isOwnerVerified", "isOpenNow", 
                "coupleMoodTypeIds", "couplePersonalityTypeIds",
                "coupleMoodTypeNames", "couplePersonalityTypeNames", 
                "venueOwnerId", "status", "createdAt", "updatedAt",
                "_geo" // Enable geo filtering
            });
            _logger.LogInformation("Updated filterable attributes (including _geo)");

            await index.UpdateSortableAttributesAsync(new[]
            {
                "averageRating", "reviewCount", "favoriteCount",
                "createdAt", "updatedAt", "priceMin", "avarageCost",
                "_geo" // Enable geo sorting
            });
            _logger.LogInformation("Updated sortable attributes (including _geo)");

            await index.UpdateDisplayedAttributesAsync(new[] { "*" });
            _logger.LogInformation("Updated displayed attributes");

            // VERIFY settings were applied
            await Task.Delay(2000); // Wait for Meilisearch to process
            var filterableAttrs = await index.GetFilterableAttributesAsync();
            var sortableAttrs = await index.GetSortableAttributesAsync();
            
            var hasGeoFilterable = filterableAttrs?.Contains("_geo") ?? false;
            var hasGeoSortable = sortableAttrs?.Contains("_geo") ?? false;
            
            _logger.LogWarning("VERIFY: _geo in filterableAttributes = {HasGeoFilterable}", hasGeoFilterable);
            _logger.LogWarning("VERIFY: _geo in sortableAttributes = {HasGeoSortable}", hasGeoSortable);

            if (!hasGeoFilterable || !hasGeoSortable)
            {
                _logger.LogError("CRITICAL: _geo missing from settings!");
                return false;
            }

            _logger.LogInformation("Meilisearch index settings configured successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring Meilisearch index settings");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<object> VerifyIndexSettingsAsync()
    {
        try
        {
            var index = _meilisearchClient.Index(_indexName);

            var filterableAttrs = await index.GetFilterableAttributesAsync();
            var sortableAttrs = await index.GetSortableAttributesAsync();
            var searchableAttrs = await index.GetSearchableAttributesAsync();

            var hasGeoFilterable = filterableAttrs?.Contains("_geo") ?? false;
            var hasGeoSortable = sortableAttrs?.Contains("_geo") ?? false;

            return new
            {
                IndexName = _indexName,
                FilterableAttributes = filterableAttrs?.ToList() ?? new List<string>(),
                SortableAttributes = sortableAttrs?.ToList() ?? new List<string>(),
                SearchableAttributes = searchableAttrs?.ToList() ?? new List<string>(),
                GeoSettings = new
                {
                    IsFilterable = hasGeoFilterable,
                    IsSortable = hasGeoSortable,
                    Status = (hasGeoFilterable && hasGeoSortable) ? "OK" : "MISSING"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Meilisearch index settings");
            return new { Error = ex.Message };
        }
    }
}
