using capstone_backend.Api.VenueRecommendation.Api.DTOs;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Recommendation;
using capstone_backend.Business.Services;
using Meilisearch;

namespace capstone_backend.Api.VenueRecommendation.Service;

public partial class MeilisearchService
{
    public async Task<VenueLocationQueryResponse> SearchVenueLocationsAsync(VenueLocationQueryRequest request, string? coupleMoodTypeName, string? memberMoodTypeName, string? couplePersonalityTypeName, string? memberMbtiType)
    {
        
        if(memberMoodTypeName != null)
            memberMoodTypeName = FaceEmotionService.MapEmotionToVietnamese(memberMoodTypeName);
        try
        {
            var index = _meilisearchClient.Index(_indexName);
            
            var (validatedMood, validatedPersonality, _, _) = ValidateFilterParameters(
                coupleMoodTypeName, memberMoodTypeName, couplePersonalityTypeName, memberMbtiType);
            
            var filters = BuildFilterString(request, validatedMood, null, validatedPersonality, null);
            var sort = BuildSortString(request);
            var offset = (request.Page - 1) * request.PageSize;
            var filterString = filters.Any() ? string.Join(" AND ", filters) : null;



            // Build attributes to retrieve - include _geoDistance when sorting by geo
            var attributesToRetrieve = new List<string> { "*" };
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                attributesToRetrieve.Add("_geoDistance");
            }
      
            var searchQuery = new SearchQuery
            {
                Offset = offset,
                Limit = request.PageSize,
                Filter = filterString,
                Sort = sort.Any() ? sort.ToArray() : null,
                AttributesToRetrieve = attributesToRetrieve.ToArray()
            };

            ISearchable<VenueLocationQueryResult> searchResult;
            
            string query = request.Query;

            if (string.IsNullOrWhiteSpace(query))
            {
                
                query = $"{validatedMood} {validatedPersonality}";
            }

            searchResult = await PerformHybridSearchAsync(query, searchQuery);

            var hits = searchResult.Hits?.ToList() ?? new List<VenueLocationQueryResult>();
            
            
            int totalHits;
            try
            {
                var totalHitsProperty = searchResult.GetType().GetProperty("TotalHits") 
                                      ?? searchResult.GetType().GetProperty("EstimatedTotalHits");
                if (totalHitsProperty != null)
                {
                    var value = totalHitsProperty.GetValue(searchResult);
                    totalHits = value != null ? Convert.ToInt32(value) : 0;
                }
                else
                {
                    totalHits = hits.Count;
                }
            }
            catch
            {
                totalHits = hits.Count;
            }
            
            if (hits.Any())
            {
                var firstHit = hits[0];
            }
            
            // Use distance from Meilisearch _geoDistance (in meters) if available, otherwise calculate manually
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                // Validate request coordinates (Vietnam: lat 8-23, lng 102-110)
                var reqLat = (double)request.Latitude.Value;
                var reqLng = (double)request.Longitude.Value;
                
                if (reqLat < -90 || reqLat > 90 || reqLng < -180 || reqLng > 180)
                {
                    _logger.LogWarning("[GEO WARNING] Invalid coordinates: lat={Lat}, lng={Lng}", reqLat, reqLng);
                }
                
                _logger.LogInformation("[GEO] Request coordinates: lat={Lat}, lng={Lng}", reqLat, reqLng);
                
                foreach (var hit in hits)
                {
                    double distanceKm;
                    
                    // Validate venue coordinates
                    if (hit.Latitude.HasValue && hit.Longitude.HasValue)
                    {
                        var venueLat = (double)hit.Latitude.Value;
                        var venueLng = (double)hit.Longitude.Value;
                        
                        if (venueLat < -90 || venueLat > 90 || venueLng < -180 || venueLng > 180)
                        {
                            _logger.LogWarning("[GEO WARNING] Venue '{Name}' has invalid coordinates: lat={Lat}, lng={Lng}", 
                                hit.Name, venueLat, venueLng);
                        }
                        
                        // Check if coordinates might be swapped (Vietnam specific)
                        if (venueLat > 50 && venueLng < 50)
                        {
                            _logger.LogWarning("[GEO WARNING] Venue '{Name}' may have SWAPPED coordinates: lat={Lat}, lng={Lng} (should be lat=8-23, lng=102-110 for Vietnam)", 
                                hit.Name, venueLat, venueLng);
                        }
                    }
                    
                    if (hit.GeoDistance.HasValue)
                    {
                        // Use distance from Meilisearch
                        distanceKm = hit.GeoDistance.Value / 1000.0;
                        _logger.LogInformation("[GEO] Venue '{Name}' (lat={VLat}, lng={VLng}) distance from Meilisearch: {Meters}m = {Km}km", 
                            hit.Name, hit.Latitude, hit.Longitude, hit.GeoDistance.Value, distanceKm);
                    }
                    else if (hit.Latitude.HasValue && hit.Longitude.HasValue)
                    {
                        // Calculate manually using Haversine formula
                        distanceKm = CalculateDistance(reqLat, reqLng, (double)hit.Latitude.Value, (double)hit.Longitude.Value);
                        _logger.LogInformation("[GEO MANUAL] Venue '{Name}' (lat={VLat}, lng={VLng}) calculated distance: {Km}km", 
                            hit.Name, hit.Latitude, hit.Longitude, distanceKm);
                    }
                    else
                    {
                        continue;
                    }
                    
                    hit.Distance = (decimal)distanceKm;
                    hit.DistanceText = FormatDistance(distanceKm);
                }
                
                // Sort results by distance (nearest first)
                hits = hits.Where(h => h.Distance.HasValue)
                           .OrderBy(h => h.Distance.Value)
                           .Concat(hits.Where(h => !h.Distance.HasValue))
                           .ToList();
            }
            
            
            // If no results with filter, try without filter to debug
            if (hits.Count == 0 && filterString != null)
            {
                _logger.LogWarning("[DEBUG] Geo filter returned 0 results. Testing without filters...");
                var debugSearch = await index.SearchAsync<VenueLocationQueryResult>("", new SearchQuery { Limit = 5 });
                _logger.LogWarning("[DEBUG] Without filters found: {Count} venues", debugSearch.Hits?.Count() ?? 0);
                
                if (debugSearch.Hits?.Any() == true)
                {
                    var firstDoc = debugSearch.Hits.First();
                    _logger.LogWarning("[DEBUG] First venue: ID={Id}, Name={Name}, HasGeo={HasGeo}, Lat={Lat}, Lng={Lng}", 
                        firstDoc.Id, firstDoc.Name, firstDoc.Geo != null, 
                        firstDoc.Latitude, firstDoc.Longitude);
                }
            }

            var pagedResult = new PagedResult<VenueLocationQueryResult>(
                hits, request.Page, request.PageSize, totalHits);

            return new VenueLocationQueryResponse
            {
                Recommendations = pagedResult,
                Explanation = BuildExplanation(request, hits.Count),
                ProcessingTimeMs = searchResult.ProcessingTimeMs,
                Query = request.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying venue locations in Meilisearch");
            
            return new VenueLocationQueryResponse
            {
                Recommendations = new PagedResult<VenueLocationQueryResult>(
                    new List<VenueLocationQueryResult>(),
                    request.Page, request.PageSize, 0),
                Explanation = "An error occurred while querying.",
                ProcessingTimeMs = 0,
                Query = request.Query
            };
        }
    }




    #region Private Search Helper Methods


    private (string? mood, string? personality, string? memberMood, string? memberMbti)  ValidateFilterParameters(
            string? coupleMoodTypeName,
            string? memberMoodTypeName,
            string? couplePersonalityTypeName,
            string? memberMbtiType)
    {
        // Validate couple mood - skip if "Không xác định" (invalid value)
        string? validatedMood = null;
        if (!string.IsNullOrWhiteSpace(coupleMoodTypeName))
        {
            if (coupleMoodTypeName == "Không xác định")
            {
                _logger.LogWarning("[VALIDATION] Invalid CoupleMoodType skipped: '{Mood}'", coupleMoodTypeName);
            }
            else
            {
                validatedMood = coupleMoodTypeName;
                _logger.LogInformation("[VALIDATION] CoupleMoodType: '{Mood}'", coupleMoodTypeName);
            }
        }

        // Validate couple personality
        string? validatedPersonality = null;
        if (!string.IsNullOrWhiteSpace(couplePersonalityTypeName))
        {
            validatedPersonality = couplePersonalityTypeName;
            _logger.LogInformation("[VALIDATION] CouplePersonalityType: '{Personality}'", couplePersonalityTypeName);
        }

        // Member mood/mbti are logged but not used in filters (no index fields)
        if (!string.IsNullOrWhiteSpace(memberMoodTypeName))
        {
            _logger.LogInformation("[VALIDATION] MemberMoodType received but not filterable: '{MemberMood}'", memberMoodTypeName);
        }

        if (!string.IsNullOrWhiteSpace(memberMbtiType))
        {
            _logger.LogInformation("[VALIDATION] MemberMbtiType received but not filterable: '{MemberMbti}'", memberMbtiType);
        }

        return (validatedMood, validatedPersonality, null, null);
    }


    private List<string> BuildFilterString(
        VenueLocationQueryRequest request, 
        string? coupleMoodTypeName = null, 
        string? memberMoodTypeName = null,
        string? couplePersonalityTypeName = null,
        string? memberMbtiType = null)
    {
        var filters = new List<string>();
        filters.Add("status = 'ACTIVE'");

        // ===== GEO FILTER (Radius-based location filter) =====
        // Only apply radius filter if explicitly provided
        if (request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue)
        {
            // Meilisearch _geoRadius format: _geoRadius(lat, lng, distance_in_meters)
            var radiusMeters = (double)request.RadiusKm.Value * 1000;
            filters.Add($"_geoRadius({request.Latitude.Value}, {request.Longitude.Value}, {radiusMeters})");
            _logger.LogInformation("[GEO FILTER] Radius: {RadiusKm} km around ({Lat}, {Lng})", 
                request.RadiusKm.Value, request.Latitude.Value, request.Longitude.Value);
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            _logger.LogInformation("[GEO SORT ONLY] Will sort by distance from ({Lat}, {Lng}) without radius filter", 
                request.Latitude.Value, request.Longitude.Value);
        }

        // ===== CHECK: User có chủ động filter không? =====
        var hasGeoFilter = request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue;
        var hasUserFilters = !string.IsNullOrWhiteSpace(request.Category)
                          || !string.IsNullOrWhiteSpace(request.Area)
                          || !string.IsNullOrWhiteSpace(request.Query)
                          || request.MinRating.HasValue
                          || request.MaxRating.HasValue
                          || request.MinPrice.HasValue
                          || request.MaxPrice.HasValue
                          || request.OnlyOpenNow == true
                          || hasGeoFilter; // ← GEO FILTER LÀ USER FILTER, BỎ QUA COUPLE CONTEXT

        if (hasUserFilters)
        {
            // ===== MODE 1: USER CHỦ ĐỘNG FILTER - Bỏ qua couple context =====
            if (!string.IsNullOrWhiteSpace(request.Category))
                filters.Add($"category = '{request.Category}'");
            
            if (!string.IsNullOrWhiteSpace(request.Area))
                filters.Add($"area = '{request.Area}'");

            if (request.MinRating.HasValue)
                filters.Add($"averageRating >= {request.MinRating.Value}");
            if (request.MaxRating.HasValue)
                filters.Add($"averageRating <= {request.MaxRating.Value}");

            if (request.MinPrice.HasValue)
                filters.Add($"priceMin >= {request.MinPrice.Value}");
            if (request.MaxPrice.HasValue)
                filters.Add($"priceMax <= {request.MaxPrice.Value}");

            if (request.OnlyOpenNow == true)
                filters.Add("isOpenNow = true");
            
            _logger.LogInformation("[FILTER MODE] User filters active - ignoring couple context");
        }
        else
        {
            // ===== MODE 2: AUTO RECOMMENDATION - Dùng couple context =====
            var coupleFilters = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(coupleMoodTypeName))
                coupleFilters.Add($"coupleMoodTypeNames = '{coupleMoodTypeName}'");
            
            if (!string.IsNullOrWhiteSpace(couplePersonalityTypeName))
                coupleFilters.Add($"couplePersonalityTypeNames = '{couplePersonalityTypeName}'");
            
            if (coupleFilters.Any())
            {
                filters.Add($"({string.Join(" OR ", coupleFilters)})"); // Đổi OR → AND nếu cần strict
                _logger.LogInformation("[FILTER MODE] Couple context recommendation");
            }
            else
            {
                _logger.LogInformation("[FILTER MODE] No filters - returning all active venues");
            }
        }

        return filters;
    }

    private List<string> BuildSortString(VenueLocationQueryRequest request)
    {
        var sort = new List<string>();

        // ===== GEO SORT (Sort by distance if user location provided) =====
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            // Meilisearch _geoPoint format: _geoPoint(lat, lng):asc
            sort.Add($"_geoPoint({request.Latitude.Value}, {request.Longitude.Value}):asc");
            _logger.LogInformation("[GEO SORT] Sorting by distance from ({Lat}, {Lng})", 
                request.Latitude.Value, request.Longitude.Value);
            return sort; // Geo sort takes priority
        }

        // ===== STANDARD SORT =====
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var direction = request.SortDirection?.ToLower() == "asc" ? "asc" : "desc";
            var validSortFields = new[] { "averageRating", "reviewCount", "createdAt", "priceMin", "favoriteCount", "avarageCost", "updatedAt" };
            
            if (validSortFields.Contains(request.SortBy.ToLower()))
                sort.Add($"{request.SortBy}:{direction}");
        }
        else
        {
            sort.Add("averageRating:desc");
        }

        return sort;
    }

    private string BuildExplanation(VenueLocationQueryRequest request, int resultCount)
    {
        return string.Empty;
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    /// <returns>Distance in kilometers</returns>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = earthRadiusKm * c;

        return Math.Round(distance, 2);
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Format distance for display (e.g., "1.2 km", "500 m")
    /// </summary>
    private string FormatDistance(double distanceKm)
    {
        if (distanceKm < 1)
        {
            var meters = (int)(distanceKm * 1000);
            return $"{meters} m";
        }
        else
        {
            return $"{distanceKm:F1} km";
        }
    }

    private async Task<ISearchable<VenueLocationQueryResult>> PerformHybridSearchAsync(
        string query,
        SearchQuery searchQuery)
    {
        var host = Environment.GetEnvironmentVariable("MEILISEARCH_HOST") 
           ?? "http://167.99.68.193:7700";

var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY") 
           ?? "Zp9xQ2f7wY8kLmN5rT4uH1cV6bA3sD0qa";

var client = new MeilisearchClient(host, apiKey);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Lower semantic ratio when sorting by distance to prioritize geo sort
        var hasGeoSort = searchQuery.Sort?.Any(s => s.Contains("_geoPoint")) ?? false;
        var semanticRatio = hasGeoSort ? 0.05 : 0.2;

        var requestBody = new
        {
            q = query,
            offset = searchQuery.Offset,
            limit = searchQuery.Limit,
            filter = searchQuery.Filter,
            sort = searchQuery.Sort,
            attributesToRetrieve = searchQuery.AttributesToRetrieve,
            hybrid = new
            {
                embedder = "venue-ai",
                semanticRatio
            }
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
        _logger.LogInformation("[AI SEARCH] Query: '{Query}', SemanticRatio: {SemanticRatio}, Embedder: venue-ai", query, semanticRatio);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{host}/indexes/{_indexName}/search", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        
        var searchResult = System.Text.Json.JsonSerializer.Deserialize<Meilisearch.SearchResult<VenueLocationQueryResult>>(
            responseBody,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (searchResult == null)
        {
            throw new InvalidOperationException("Failed to deserialize Meilisearch hybrid search response");
        }

        return searchResult;
    }

    #endregion
}
