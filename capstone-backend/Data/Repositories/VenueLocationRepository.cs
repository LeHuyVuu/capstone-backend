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

    public async Task<VenueLocation?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Where(vl => vl.Id == id && vl.IsDeleted == false)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get venue location by ID with all related entities and opening hours for today
    /// </summary>
    public async Task<VenueLocation?> GetByIdWithDetailsAsync(int id)
    {
        var todayDbFormat = GetTodayDayOfWeek();

        return await _dbSet
            .AsNoTracking()
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CouplePersonalityType)
            .Include(v => v.VenueOwner)
            .Include(v => v.VenueOpeningHours.Where(oh => oh.Day == todayDbFormat))
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id && v.IsDeleted != true);
    }

    /// <summary>
    /// Get today's day of week in database format (2=Monday, 3=Tuesday, ..., 8=Sunday)
    /// </summary>
    private static int GetTodayDayOfWeek()
    {
        var today = DateTime.UtcNow.AddHours(7).DayOfWeek;
        return today switch
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
    /// Get venue locations by venue owner ID with LocationTag details
    /// Including CoupleMoodType and CouplePersonalityType information
    /// </summary>
    public async Task<List<VenueLocation>> GetByVenueOwnerIdWithLocationTagAsync(int venueOwnerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CouplePersonalityType)
            .Where(v => v.VenueOwnerId == venueOwnerId && v.IsDeleted != true)
            .OrderByDescending(v => v.CreatedAt)
            .AsSplitQuery()
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
    /// Supports: lat/lon radius search OR area (province/city) filtering
    /// Returns venues with distance calculated using Haversine formula
    /// </summary>
    public async Task<List<(VenueLocation Venue, decimal? DistanceKm)>> GetForRecommendationsAsync(
        string? coupleMoodType,
        List<string> personalityTags,
        string? singleMoodName,
        string? area,
        decimal? latitude,
        decimal? longitude,
        decimal? radiusKm,
        int limit,
        int? budgetLevel)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(v => v.IsDeleted != true && v.Status == "ACTIVE");

        bool hasGeoFilter = latitude.HasValue && longitude.HasValue;

        // PRIORITY 1: Direct lat/lon radius search (most accurate)
        if (hasGeoFilter)
        {
            // Calculate bounding box for faster query (approximate)
            // 1 degree latitude ≈ 111km, 1 degree longitude ≈ 111km * cos(latitude)
            var radius = radiusKm ?? 5m; // Default 5km
            var latDelta = radius / 111m;
            var lonDelta = radius / (111m * (decimal)Math.Cos((double)latitude!.Value * Math.PI / 180));

            var minLat = latitude.Value - latDelta;
            var maxLat = latitude.Value + latDelta;
            var minLon = longitude!.Value - lonDelta;
            var maxLon = longitude.Value + lonDelta;

            query = query.Where(v => 
                v.Latitude != null && v.Longitude != null &&
                v.Latitude >= minLat && v.Latitude <= maxLat &&
                v.Longitude >= minLon && v.Longitude <= maxLon
            );
        }
        // PRIORITY 2: Filter by Area (province code) when no lat/lon
        // Area is province code: "01" = Hà Nội, "79" = TP.HCM, "92" = Cần Thơ, etc.
        else if (!string.IsNullOrEmpty(area))
        {
            query = query.Where(v => v.Area == area);
        }

        // PRIORITY 3: Filter by Budget (Average Cost)
        if (budgetLevel.HasValue)
        {
            query = budgetLevel.Value switch
            {
                1 => query.Where(v => v.AvarageCost < 200000),             // Low: < 200k
                2 => query.Where(v => v.AvarageCost >= 200000 && v.AvarageCost <= 1000000), // Medium: 200k - 1m
                3 => query.Where(v => v.AvarageCost > 1000000),            // High: > 1m
                _ => query
            };
        }
       
        // Then apply mood/personality filters (if specified)
        bool hasFilters = !string.IsNullOrEmpty(coupleMoodType) || !string.IsNullOrEmpty(singleMoodName) || personalityTags.Any();
        
        if (hasFilters)
        {
            query = query
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CouplePersonalityType)
                .Where(v => v.VenueLocationTags.Any());

            // Build filter conditions
            if (!string.IsNullOrEmpty(singleMoodName))
            {
                // Single person: filter mood bằng DetailTag Contains
                query = query.Where(v =>
                    v.VenueLocationTags.Any(vlt => 
                        (vlt.LocationTag.DetailTag != null && vlt.LocationTag.DetailTag.Contains(singleMoodName)) ||
                        (personalityTags.Any() && vlt.LocationTag.CouplePersonalityType != null &&
                         vlt.LocationTag.CouplePersonalityType.Name != null &&
                         personalityTags.Contains(vlt.LocationTag.CouplePersonalityType.Name))
                    )
                );
            }
            else if (!string.IsNullOrEmpty(coupleMoodType) || personalityTags.Any())
            {
                // Couple: filter mood bằng CoupleMoodType.Name
                query = query.Where(v =>
                    v.VenueLocationTags.Any(vlt => 
                        (coupleMoodType != null && vlt.LocationTag.CoupleMoodType != null && 
                         vlt.LocationTag.CoupleMoodType.Name == coupleMoodType) ||
                        (personalityTags.Any() && vlt.LocationTag.CouplePersonalityType != null &&
                         vlt.LocationTag.CouplePersonalityType.Name != null &&
                         personalityTags.Contains(vlt.LocationTag.CouplePersonalityType.Name))
                    )
                );
            }
        }
        else
        {
            // No filters - include relationships for display
            query = query
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CouplePersonalityType);
        }

        // Always include reviews for rating calculation
        query = query.Include(v => v.Reviews);

        // Fetch venues from database
        var venues = await query
            .AsSplitQuery()
            .ToListAsync();

        // Calculate distance and sort if geo filter is provided
        if (hasGeoFilter)
        {
            var venuesWithDistance = venues
                .Select(v => (
                    Venue: v,
                    DistanceKm: CalculateHaversineDistance(
                        latitude!.Value, longitude!.Value,
                        v.Latitude ?? 0, v.Longitude ?? 0
                    )
                ))
                .Where(x => x.DistanceKm <= (radiusKm ?? 5m)) // Filter by actual distance
                .OrderBy(x => x.DistanceKm) // Sort by distance (nearest first)
                .Take(limit)
                .Select(x => (x.Venue, (decimal?)x.DistanceKm))
                .ToList();

            return venuesWithDistance;
        }

        // No geo filter - return without distance
        return venues
            .Take(limit)
            .Select(v => (v, (decimal?)null))
            .ToList();
    }


    private static decimal CalculateHaversineDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = EarthRadiusKm * c;

        return (decimal)Math.Round(distance, 2);
    }


    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
    /// <summary>
    /// Get pending venue locations for admin review
    /// </summary>
    public async Task<(List<VenueLocation> Venues, int TotalCount)> GetPendingVenuesAsync(int page, int pageSize)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(v => v.VenueOwner)
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(v => v.VenueLocationTags)
                .ThenInclude(vlt => vlt.LocationTag)
                    .ThenInclude(lt => lt!.CouplePersonalityType)
            .Where(v => v.Status == "PENDING" && v.IsDeleted != true);

        var totalCount = await query.CountAsync();

        var venues = await query
            .OrderByDescending(v => v.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return (venues, totalCount);
    }

    public async Task<VenueLocation?> GetByIdWithOwnerAsync(int id)
    {
        return await _dbSet
            .Include(vl => vl.VenueOwner)
            .FirstOrDefaultAsync(vl => vl.Id == id);
    }
}
