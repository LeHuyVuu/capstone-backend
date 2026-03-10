using Meilisearch;

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

            await index.UpdateSearchableAttributesAsync(new[]
            {
                "name", "description", "address", "category", "area",
                "coupleMoodTypeNames", "couplePersonalityTypeNames", "venueOwnerName"
            });

            await index.UpdateFilterableAttributesAsync(new[]
            {
                "id", "category", "area", "averageRating", "priceMin", "priceMax",
                "isOwnerVerified", "isOpenNow", 
                "coupleMoodTypeIds", "couplePersonalityTypeIds",
                "coupleMoodTypeNames", "couplePersonalityTypeNames", 
                "venueOwnerId", "status", "createdAt", "updatedAt",
                "_geo" // Enable geo filtering
            });

            await index.UpdateSortableAttributesAsync(new[]
            {
                "averageRating", "reviewCount", "favoriteCount",
                "createdAt", "updatedAt", "priceMin", "avarageCost",
                "_geo" // Enable geo sorting
            });

            await index.UpdateDisplayedAttributesAsync(new[] { "*" });

            await index.UpdateTypoToleranceAsync(new Meilisearch.TypoTolerance { Enabled = true });

            await Task.Delay(2000);
            var filterableAttrs = await index.GetFilterableAttributesAsync();
            var sortableAttrs = await index.GetSortableAttributesAsync();
            
            var hasGeoFilterable = filterableAttrs?.Contains("_geo") ?? false;
            var hasGeoSortable = sortableAttrs?.Contains("_geo") ?? false;
            

            if (!hasGeoFilterable || !hasGeoSortable)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
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
