using capstone_backend.Api.VenueRecommendation.Api.DTOs;
using capstone_backend.Data.Enums;
using Meilisearch;
using Microsoft.EntityFrameworkCore;

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
            var venue = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .AsNoTracking()
                .Include(v => v.VenueOwner)
                .Include(v => v.VenueOpeningHours)
                .Include(v => v.VenueLocationCategories)
                    .ThenInclude(vlc => vlc.Category)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
                .FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null || venue.IsDeleted == true)
            {
                _logger.LogWarning("Venue location {VenueId} not found or is deleted", venueId);
                return false;
            }

            var document = await MapToQueryResultAsync(venue);
            var index = _meilisearchClient.Index(_indexName);
            await index.AddDocumentsAsync(new[] { document }, "id");
            _logger.LogInformation("Indexed venue location {VenueId} '{VenueName}' to Meilisearch with category: '{Category}'", 
                venueId, document.Name, document.Category ?? "(null)");
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
            var venues = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .AsNoTracking()
                .Include(v => v.VenueOwner)
                .Include(v => v.VenueOpeningHours)
                .Include(v => v.VenueLocationCategories)
                    .ThenInclude(vlc => vlc.Category)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
                .Where(v => v.IsDeleted != true &&
                            (v.Status == VenueLocationStatus.ACTIVE.ToString() ||
                             v.Status == VenueLocationStatus.INACTIVE.ToString()))
                .ToListAsync();

            var documents = new List<VenueLocationQueryResult>();

            foreach (var venue in venues)
            {
                var document = await MapToQueryResultAsync(venue);
                documents.Add(document);
            }

            var index = _meilisearchClient.Index(_indexName);
            await index.AddDocumentsAsync(documents, "id");
            _logger.LogInformation("Successfully indexed {Count} venue locations to Meilisearch", documents.Count);
            return documents.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing all venue locations to Meilisearch");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<int> IndexAllVenueLocationsV2Async()
    {
        try
        {
            var venues = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .AsNoTracking()
                .Include(v => v.VenueOwner)
                .Include(v => v.VenueOpeningHours)
                .Include(v => v.VenueLocationCategories)
                    .ThenInclude(vlc => vlc.Category)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
                .Where(v => v.IsDeleted != true &&
                            (v.Status == VenueLocationStatus.ACTIVE.ToString()))
                .ToListAsync();

            var documents = new List<VenueLocationQueryResult>();

            foreach (var venue in venues)
            {
                var document = await MapToQueryResultAsync(venue);
                documents.Add(document);
            }

            var v2Host = Environment.GetEnvironmentVariable("MEILISEARCH_V2_HOST")
                         ?? "http://134.209.108.208:7700";

            var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                         ?? "couplemood123";

            var v2Client = new MeilisearchClient(v2Host, apiKey);
            var index = v2Client.Index(_indexName);
            await index.AddDocumentsAsync(documents, "id");

            _logger.LogInformation(
                "Successfully indexed {Count} venue locations to Meilisearch v2 host {Host}",
                documents.Count,
                v2Host);

            return documents.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing all venue locations to Meilisearch v2 host");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IndexVenueLocationV2Async(int venueId)
    {
        try
        {
            var venue = await _unitOfWork.Context.Set<Data.Entities.VenueLocation>()
                .AsNoTracking()
                .Include(v => v.VenueOwner)
                .Include(v => v.VenueOpeningHours)
                .Include(v => v.VenueLocationCategories)
                    .ThenInclude(vlc => vlc.Category)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vt => vt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
                .FirstOrDefaultAsync(v => v.Id == venueId);

            if (venue == null || venue.IsDeleted == true)
            {
                _logger.LogWarning("Venue location {VenueId} not found or is deleted", venueId);
                return false;
            }

            var document = await MapToQueryResultAsync(venue);
            var v2Host = Environment.GetEnvironmentVariable("MEILISEARCH_V2_HOST")
                         ?? "http://134.209.108.208:7700";
            var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                         ?? "couplemood123";

            var v2Client = new MeilisearchClient(v2Host, apiKey);
            var index = v2Client.Index(_indexName);
            await index.AddDocumentsAsync(new[] { document }, "id");

            _logger.LogInformation("Indexed venue location {VenueId} to Meilisearch v2 host {Host}", venueId, v2Host);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing venue location {VenueId} to Meilisearch v2 host", venueId);
            return false;
        }
    }
}
