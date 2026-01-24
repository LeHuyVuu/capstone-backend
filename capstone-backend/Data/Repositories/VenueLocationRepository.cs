using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for VenueLocation entity
/// </summary>
public class VenueLocationRepository : GenericRepository<VenueLocation>, IVenueLocationRepository
{
    public VenueLocationRepository(MyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get venue location by ID with all related entities (LocationTag, CoupleMoodType, CouplePersonalityType, VenueOwner)
    /// </summary>
    public async Task<VenueLocation?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.LocationTag)
                .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(v => v.LocationTag)
                .ThenInclude(lt => lt!.CouplePersonalityType)
            .Include(v => v.VenueOwner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id && v.IsDeleted != true);
    }

    /// <summary>
    /// Get venue locations by venue owner ID
    /// </summary>
    public async Task<List<VenueLocation>> GetByVenueOwnerIdAsync(int venueOwnerId)
    {
        return await _dbSet
            .Where(v => v.VenueOwnerId == venueOwnerId && v.IsDeleted != true)
            .ToListAsync();
    }

    /// <summary>
    /// Get reviews for a venue with member and user information
    /// </summary>
    public async Task<(List<Review> Reviews, int TotalCount)> GetReviewsByVenueIdAsync(int venueId, int page, int pageSize)
    {
        var query = _context.Set<Review>()
            .AsNoTracking()
            .Include(r => r.Member)
                .ThenInclude(m => m.User)
            .Where(r => r.VenueId == venueId && r.IsDeleted != true);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return (reviews, totalCount);
    }

    /// <summary>
    /// Get venues for recommendations with mood/personality filtering
    /// OPTIMIZED: Geo-location based filtering using lat/lon bounding boxes or radius search
    /// </summary>
    public async Task<List<VenueLocation>> GetForRecommendationsAsync(
        string? coupleMoodType,
        List<string> personalityTags,
        string? region,
        decimal? latitude,
        decimal? longitude,
        decimal? radiusKm,
        int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(v => v.IsDeleted != true);

        // PRIORITY 1: Direct lat/lon radius search (most accurate)
        if (latitude.HasValue && longitude.HasValue)
        {
            // Calculate bounding box for faster query (approximate)
            // 1 degree latitude ≈ 111km, 1 degree longitude ≈ 111km * cos(latitude)
            var radius = radiusKm ?? 5m; // Default 5km
            var latDelta = radius / 111m;
            var lonDelta = radius / (111m * (decimal)Math.Cos((double)latitude.Value * Math.PI / 180));

            var minLat = latitude.Value - latDelta;
            var maxLat = latitude.Value + latDelta;
            var minLon = longitude.Value - lonDelta;
            var maxLon = longitude.Value + lonDelta;

            query = query.Where(v => 
                v.Latitude != null && v.Longitude != null &&
                v.Latitude >= minLat && v.Latitude <= maxLat &&
                v.Longitude >= minLon && v.Longitude <= maxLon
            );
        }
        // PRIORITY 2: Region string with predefined bounding boxes
        else if (!string.IsNullOrEmpty(region))
        {
            var normalizedRegion = region.Trim().ToLower();
            
            // Define bounding boxes for major regions (lat/lon ranges)
            // Format: (minLat, maxLat, minLon, maxLon)
            if (normalizedRegion.Contains("hà nội") || normalizedRegion.Contains("ha noi") || normalizedRegion.Contains("hanoi"))
            {
                // Hà Nội bounding box: 20.9°N - 21.3°N, 105.6°E - 106.0°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 20.9m && v.Latitude <= 21.3m &&
                    v.Longitude >= 105.6m && v.Longitude <= 106.0m
                );
            }
            else if (normalizedRegion.Contains("hồ chí minh") || normalizedRegion.Contains("hcm") || 
                     normalizedRegion.Contains("tp.hcm") || normalizedRegion.Contains("sài gòn") ||
                     normalizedRegion.Contains("saigon") || normalizedRegion.Contains("thủ đức"))
            {
                // TP.HCM + Thủ Đức bounding box: 10.6°N - 11.0°N, 106.4°E - 107.0°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 10.6m && v.Latitude <= 11.0m &&
                    v.Longitude >= 106.4m && v.Longitude <= 107.0m
                );
            }
            else if (normalizedRegion.Contains("đà nẵng") || normalizedRegion.Contains("da nang") || normalizedRegion.Contains("danang"))
            {
                // Đà Nẵng bounding box: 15.9°N - 16.2°N, 107.9°E - 108.3°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 15.9m && v.Latitude <= 16.2m &&
                    v.Longitude >= 107.9m && v.Longitude <= 108.3m
                );
            }
            else if (normalizedRegion.Contains("nha trang"))
            {
                // Nha Trang bounding box: 12.1°N - 12.4°N, 109.1°E - 109.3°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 12.1m && v.Latitude <= 12.4m &&
                    v.Longitude >= 109.1m && v.Longitude <= 109.3m
                );
            }
            else if (normalizedRegion.Contains("hải phòng") || normalizedRegion.Contains("hai phong"))
            {
                // Hải Phòng bounding box: 20.7°N - 20.9°N, 106.5°E - 106.8°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 20.7m && v.Latitude <= 20.9m &&
                    v.Longitude >= 106.5m && v.Longitude <= 106.8m
                );
            }
            else if (normalizedRegion.Contains("cần thơ") || normalizedRegion.Contains("can tho"))
            {
                // Cần Thơ bounding box: 10.0°N - 10.2°N, 105.7°E - 105.9°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 10.0m && v.Latitude <= 10.2m &&
                    v.Longitude >= 105.7m && v.Longitude <= 105.9m
                );
            }
            else if (normalizedRegion.Contains("huế") || normalizedRegion.Contains("hue"))
            {
                // Huế bounding box: 16.4°N - 16.5°N, 107.5°E - 107.7°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 16.4m && v.Latitude <= 16.5m &&
                    v.Longitude >= 107.5m && v.Longitude <= 107.7m
                );
            }
            else if (normalizedRegion.Contains("vũng tàu") || normalizedRegion.Contains("vung tau"))
            {
                // Vũng Tàu bounding box: 10.3°N - 10.5°N, 107.0°E - 107.2°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 10.3m && v.Latitude <= 10.5m &&
                    v.Longitude >= 107.0m && v.Longitude <= 107.2m
                );
            }
            else if (normalizedRegion.Contains("đà lạt") || normalizedRegion.Contains("da lat") || normalizedRegion.Contains("dalat"))
            {
                // Đà Lạt bounding box: 11.8°N - 12.0°N, 108.3°E - 108.5°E
                query = query.Where(v => 
                    v.Latitude != null && v.Longitude != null &&
                    v.Latitude >= 11.8m && v.Latitude <= 12.0m &&
                    v.Longitude >= 108.3m && v.Longitude <= 108.5m
                );
            }
            else
            {
                // Fallback: Address string matching for unknown regions
                query = query.Where(v => 
                    v.Address.ToLower().Contains(normalizedRegion)
                );
            }
        }

        // Then apply mood/personality filters (if specified)
        if (!string.IsNullOrEmpty(coupleMoodType) || personalityTags.Any())
        {
            query = query
                .Include(v => v.LocationTag!)
                    .ThenInclude(lt => lt!.CoupleMoodType)
                .Include(v => v.LocationTag!)
                    .ThenInclude(lt => lt!.CouplePersonalityType)
                .Where(v =>
                    v.LocationTag != null && (
                        (coupleMoodType != null && v.LocationTag.CoupleMoodType != null && 
                         v.LocationTag.CoupleMoodType.Name == coupleMoodType) ||
                        (personalityTags.Any() && v.LocationTag.CouplePersonalityType != null &&
                         v.LocationTag.CouplePersonalityType.Name != null &&
                         personalityTags.Contains(v.LocationTag.CouplePersonalityType.Name))
                    )
                );
        }
        else
        {
            // No filters - include relationships for display
            query = query
                .Include(v => v.LocationTag!)
                    .ThenInclude(lt => lt!.CoupleMoodType)
                .Include(v => v.LocationTag!)
                    .ThenInclude(lt => lt!.CouplePersonalityType);
        }

        // Always include reviews for rating calculation
        query = query.Include(v => v.Reviews);

        return await query
            .Take(limit)
            .AsSplitQuery()
            .ToListAsync();
    }
}
