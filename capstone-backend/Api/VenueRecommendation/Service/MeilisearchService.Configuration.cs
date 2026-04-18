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
        return await ConfigureIndexSettingsInternalAsync(_meilisearchClient, "default");
    }

    /// <inheritdoc/>
    public async Task<bool> ConfigureIndexSettingsV2Async()
    {
        var v2Host = Environment.GetEnvironmentVariable("MEILISEARCH_V2_HOST")
                     ?? "http://134.209.108.208:7700";

        var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                     ?? "couplemood123";

        var v2Client = new MeilisearchClient(v2Host, apiKey);
        return await ConfigureIndexSettingsInternalAsync(v2Client, "v2");
    }

    private async Task<bool> ConfigureIndexSettingsInternalAsync(MeilisearchClient client, string hostLabel)
    {
        try
        {
            var index = client.Index(_indexName);

            // Ranking Rules: Ưu tiên exact match và relevance
            await index.UpdateRankingRulesAsync(new[]
            {
                "words",        // Số từ match
                "typo",         // Ít typo hơn
                "proximity",    // Từ gần nhau
                "attribute",    // Match ở attribute quan trọng (theo order searchable)
                "sort",         // Custom sort
                "exactness"     // Exact match được ưu tiên nhất
            });

            // Searchable Attributes: Order = Weight (đầu = quan trọng nhất)
            await index.UpdateSearchableAttributesAsync(new[]
            {
                "name",                       // Tên venue - ưu tiên cao nhất cho text search
                "category",                   // Loại hình - ưu tiên cao thứ 2
                "description",                // Mô tả
                "coupleMoodTypeNames",        // Mood context
                "couplePersonalityTypeNames", // Personality context
                "detailTags",                 // Tags chi tiết
                "address",                    // Địa chỉ
                "area",                       // Khu vực
                "venueOwnerName"              // Chủ venue
            });

            await index.UpdateFilterableAttributesAsync(new[]
            {
                "id", "category", "area", "averageRating", "priceMin", "priceMax",
                "isOwnerVerified", "isOpenNow", 
                "coupleMoodTypeIds", "couplePersonalityTypeIds",
                "coupleMoodTypeNames", "couplePersonalityTypeNames",
                "detailTags",  // Filterable tags
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

            await index.UpdateSynonymsAsync(new Dictionary<string, IEnumerable<string>>
            {
                // Venue types
                { "cafe", new[] { "cafe", "cà phê", "ca phe", "quán cà phê", "quan ca phe", "coffee", "coffee shop" } },
                { "restaurant", new[] { "restaurant", "nhà hàng", "nha hang", "quán ăn", "quan an", "dining" } },
                { "bar", new[] { "bar", "quầy bar", "quay bar", "pub" } },
                { "seafood", new[] { "seafood", "hải sản", "hai san", "seafoods" } },
                { "park", new[] { "park", "công viên", "cong vien", "garden", "vườn" } },
                { "museum", new[] { "museum", "bảo tàng", "bao tang", "gallery" } },
                { "beach", new[] { "beach", "bãi biển", "bai bien", "seaside", "coast" } },
                { "mountain", new[] { "mountain", "núi", "đồi", "doi", "hill" } },
                
                // Moods & Emotions
                { "romantic", new[] { "romantic", "lãng mạn", "lang man", "romanticism" } },
                { "couple", new[] { "couple", "cặp đôi", "cap doi", "lovers", "partners" } },
                { "vui vẻ", new[] { "vui vẻ", "vui ve", "happy", "cheerful", "joyful" } },
                { "cân bằng", new[] { "cân bằng", "can bang", "balanced", "calm", "peaceful", "yên tĩnh", "yen tinh" } },
                { "hòa giải", new[] { "hòa giải", "hoa giai", "reconcile", "harmony" } },
                { "lãng mạn", new[] { "lãng mạn", "lang man", "romantic", "romance" } },
                { "vui nhộn", new[] { "vui nhộn", "vui nhon", "playful", "fun", "funny" } },
                { "phiêu lưu", new[] { "phiêu lưu", "phieu luu", "adventure", "adventurous" } },
                { "yên tĩnh", new[] { "yên tĩnh", "yen tinh", "quiet", "calm", "peaceful", "cân bằng", "can bang" } }
            });

            await Task.Delay(2000);
            var filterableAttrs = await index.GetFilterableAttributesAsync();
            var sortableAttrs = await index.GetSortableAttributesAsync();
            
            var hasGeoFilterable = filterableAttrs?.Contains("_geo") ?? false;
            var hasGeoSortable = sortableAttrs?.Contains("_geo") ?? false;
            

            if (!hasGeoFilterable || !hasGeoSortable)
            {
                _logger.LogWarning("Meilisearch {HostLabel} index settings missing geo capability for index {Index}", hostLabel, _indexName);
                return false;
            }

            _logger.LogInformation("Configured Meilisearch {HostLabel} index settings successfully for index {Index}", hostLabel, _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring Meilisearch {HostLabel} index settings for index {Index}", hostLabel, _indexName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<object> VerifyIndexSettingsAsync()
    {
        return await VerifyIndexSettingsInternalAsync(_meilisearchClient, "default");
    }

    /// <inheritdoc/>
    public async Task<object> VerifyIndexSettingsV2Async()
    {
        var v2Host = Environment.GetEnvironmentVariable("MEILISEARCH_V2_HOST")
                     ?? "http://134.209.108.208:7700";

        var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                     ?? "couplemood123";

        var v2Client = new MeilisearchClient(v2Host, apiKey);
        return await VerifyIndexSettingsInternalAsync(v2Client, "v2");
    }

    private async Task<object> VerifyIndexSettingsInternalAsync(MeilisearchClient client, string hostLabel)
    {
        try
        {
            var index = client.Index(_indexName);

            var filterableAttrs = await index.GetFilterableAttributesAsync();
            var sortableAttrs = await index.GetSortableAttributesAsync();
            var searchableAttrs = await index.GetSearchableAttributesAsync();
            var synonyms = await index.GetSynonymsAsync();

            var hasGeoFilterable = filterableAttrs?.Contains("_geo") ?? false;
            var hasGeoSortable = sortableAttrs?.Contains("_geo") ?? false;

            return new
            {
                HostLabel = hostLabel,
                IndexName = _indexName,
                FilterableAttributes = filterableAttrs?.ToList() ?? new List<string>(),
                SortableAttributes = sortableAttrs?.ToList() ?? new List<string>(),
                SearchableAttributes = searchableAttrs?.ToList() ?? new List<string>(),
                Synonyms = synonyms ?? new Dictionary<string, IEnumerable<string>>(),
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
            _logger.LogError(ex, "Error verifying Meilisearch {HostLabel} index settings", hostLabel);
            return new { Error = ex.Message };
        }
    }
}
