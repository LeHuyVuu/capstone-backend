using capstone_backend.Api.VenueRecommendation.Api.DTOs;
using Newtonsoft.Json;

namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Meilisearch service - Helper methods
/// </summary>
public partial class MeilisearchService
{
    private async Task<VenueLocationQueryResult> MapToQueryResultAsync(Data.Entities.VenueLocation venue)
    {
        var coverImages = ParseImageList(venue.CoverImage);
        var interiorImages = ParseImageList(venue.InteriorImage);
        var menuImages = ParseImageList(venue.FullPageMenuImage);

        var moodTypeIds = new List<int>();
        var moodTypeNames = new List<string>();
        var personalityTypeIds = new List<int>();
        var personalityTypeNames = new List<string>();

        if (venue.VenueLocationTags?.Any() == true)
        {
            foreach (var venueTag in venue.VenueLocationTags.Where(vt => vt.IsDeleted != true))
            {
                var locationTag = venueTag.LocationTag;
                if (locationTag == null) continue;

                if (locationTag.CoupleMoodTypeId.HasValue && locationTag.CoupleMoodType != null)
                {
                    if (!moodTypeIds.Contains(locationTag.CoupleMoodTypeId.Value))
                    {
                        moodTypeIds.Add(locationTag.CoupleMoodTypeId.Value);
                        if (!string.IsNullOrWhiteSpace(locationTag.CoupleMoodType.Name))
                            moodTypeNames.Add(locationTag.CoupleMoodType.Name);
                    }
                }

                if (locationTag.CouplePersonalityTypeId.HasValue && locationTag.CouplePersonalityType != null)
                {
                    if (!personalityTypeIds.Contains(locationTag.CouplePersonalityTypeId.Value))
                    {
                        personalityTypeIds.Add(locationTag.CouplePersonalityTypeId.Value);
                        if (!string.IsNullOrWhiteSpace(locationTag.CouplePersonalityType.Name))
                            personalityTypeNames.Add(locationTag.CouplePersonalityType.Name);
                    }
                }
            }
        }

        var (isOpenNow, todayOpenTime, todayCloseTime) = GetTodayOpeningStatus(venue.VenueOpeningHours);

        // Get category from VenueLocationCategories if available, otherwise use Category field
        string? categoryValue = venue.Category;
        if (venue.VenueLocationCategories?.Any() == true)
        {
            var firstCategory = venue.VenueLocationCategories
                .Where(vlc => vlc.IsDeleted != true)
                .Select(vlc => vlc.Category?.Name)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
            
            if (!string.IsNullOrWhiteSpace(firstCategory))
            {
                categoryValue = firstCategory;
            }
        }
        
        if (string.IsNullOrWhiteSpace(categoryValue))
        {
            _logger.LogWarning("[CATEGORY MAPPING] Venue {VenueId} '{VenueName}' has no category", 
                venue.Id, venue.Name);
        }

        return new VenueLocationQueryResult
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = venue.Address,
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Geo = (venue.Latitude.HasValue && venue.Longitude.HasValue) 
                ? new GeoPoint 
                { 
                    Lat = (double)venue.Latitude.Value, 
                    Lng = (double)venue.Longitude.Value 
                } 
                : null,
            Area = venue.Area,
            AverageRating = venue.AverageRating,
            AvarageCost = venue.AvarageCost,
            ReviewCount = venue.ReviewCount,
            FavoriteCount = venue.FavoriteCount,
            Status = venue.Status,
            CoverImage = coverImages,
            InteriorImage = interiorImages,
            Category = categoryValue,
            FullPageMenuImage = menuImages,
            IsOwnerVerified = venue.IsOwnerVerified,
            CreatedAt = venue.CreatedAt.HasValue ? new DateTimeOffset(venue.CreatedAt.Value).ToUnixTimeSeconds() : null,
            UpdatedAt = venue.UpdatedAt.HasValue ? new DateTimeOffset(venue.UpdatedAt.Value).ToUnixTimeSeconds() : null,
            VenueOwnerId = venue.VenueOwnerId,
            VenueOwnerName = venue.VenueOwner?.BusinessName,
            CoupleMoodTypeIds = moodTypeIds.Any() ? moodTypeIds : null,
            CoupleMoodTypeNames = moodTypeNames.Any() ? moodTypeNames : null,
            CouplePersonalityTypeIds = personalityTypeIds.Any() ? personalityTypeIds : null,
            CouplePersonalityTypeNames = personalityTypeNames.Any() ? personalityTypeNames : null,
            IsOpenNow = isOpenNow,
            TodayOpenTime = todayOpenTime,
            TodayCloseTime = todayCloseTime
        };
    }

    private List<string>? ParseImageList(string? imageString)
    {
        if (string.IsNullOrWhiteSpace(imageString))
            return null;

        try
        {
            // Handle database format: '[ "url1", "url2" ]' 
            // Remove outer quotes first
            var trimmed = imageString.Trim();
            
            // Remove leading/trailing single or double quotes
            if ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
                (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }
            
            // Now check if it's a JSON array
            trimmed = trimmed.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                // Unescape any escaped quotes before deserializing
                trimmed = trimmed.Replace("\\\"", "\"");
                
                var deserializedArray = JsonConvert.DeserializeObject<List<string>>(trimmed);
                if (deserializedArray != null && deserializedArray.Any())
                {
                    _logger.LogInformation("[IMAGE PARSE] Successfully parsed JSON array with {Count} images", deserializedArray.Count);
                    return deserializedArray
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IMAGE PARSE] JSON deserialization failed, falling back to comma-separated parsing. Input: {Input}", 
                imageString?.Substring(0, Math.Min(100, imageString.Length)));
        }

        // Fallback: parse as comma-separated string
        _logger.LogWarning("[IMAGE PARSE] Using comma-separated fallback parsing");
        return (imageString ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().Trim('\'', '"', '[', ']', '\\'))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private (bool? isOpenNow, string? openTime, string? closeTime) GetTodayOpeningStatus(
        ICollection<Data.Entities.VenueOpeningHour>? openingHours)
    {
        if (openingHours == null || !openingHours.Any())
            return (null, null, null);

        var today = DateTime.UtcNow.AddHours(7).DayOfWeek;
        var todayDay = today switch
        {
            DayOfWeek.Sunday => 8,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            DayOfWeek.Saturday => 7,
            _ => 8
        };

        var todayHour = openingHours.FirstOrDefault(h => h.Day == todayDay);
        if (todayHour == null)
            return (null, null, null);

        if (todayHour.IsClosed == true)
            return (false, null, null);

        var now = DateTime.UtcNow.AddHours(7).TimeOfDay;
        var isOpen = now >= todayHour.OpenTime && now <= todayHour.CloseTime;

        return (isOpen,
            todayHour.OpenTime.ToString(@"hh\:mm"),
            todayHour.CloseTime.ToString(@"hh\:mm"));
    }
}
