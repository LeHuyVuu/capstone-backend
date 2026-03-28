using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class SubscriptionPackageService : ISubscriptionPackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionPackageService> _logger;

    public SubscriptionPackageService(
        IUnitOfWork unitOfWork, 
        ILogger<SubscriptionPackageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<SubscriptionPackageDto>> GetSubscriptionPackagesByTypeAsync(string type)
    {
        try
        {
            // Validate type
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Type cannot be null or empty", nameof(type));
            }

            // Normalize type to uppercase
            var normalizedType = type.ToUpper().Trim();

            // Validate type is either MEMBER or VENUE
            if (normalizedType != "MEMBER" && normalizedType != "VENUE" && normalizedType != "VENUEOWNER")
            {
                throw new ArgumentException("Type must be either MEMBER, VENUE, or VENUEOWNER", nameof(type));
            }

            _logger.LogInformation("Getting subscription packages for type: {Type}", normalizedType);

            // Query subscription packages by type
            var packages = await _unitOfWork.Context.Set<SubscriptionPackage>()
                .Where(p => p.Type == normalizedType && 
                           p.IsDeleted != true && 
                           p.IsActive == true)
                .OrderBy(p => p.Price)
                .Select(p => new SubscriptionPackageDto
                {
                    Id = p.Id,
                    PackageName = p.PackageName,
                    Price = p.Price,
                    DurationDays = p.DurationDays,
                    Type = p.Type,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} subscription packages for type {Type}", 
                packages.Count, 
                normalizedType);

            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription packages for type: {Type}", type);
            throw;
        }
    }

    public async Task<SubscriptionPackageDto> UpdateSubscriptionPackageAsync(
        int id, 
        UpdateSubscriptionPackageRequest request)
    {
        try
        {
            _logger.LogInformation("Updating subscription package with ID: {Id}", id);

            // Find the existing package
            var package = await _unitOfWork.Context.Set<SubscriptionPackage>()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted != true);

            if (package == null)
            {
                throw new InvalidOperationException($"Subscription package with ID {id} not found or has been deleted");
            }

            var normalizedPackageName = request.PackageName.Trim();
            var duplicatedNameExists = await _unitOfWork.Context.Set<SubscriptionPackage>()
                .AnyAsync(p => p.Id != id
                               && p.IsDeleted != true
                               && p.PackageName != null
                               && p.PackageName.ToUpper() == normalizedPackageName.ToUpper());

            if (duplicatedNameExists)
            {
                throw new InvalidOperationException("Package name already exists. PackageName must be unique.");
            }

            // Update package properties
            package.PackageName = normalizedPackageName;
            package.Price = request.Price;
            package.DurationDays = request.DurationDays;
            package.Description = request.Description;
            package.IsActive = request.IsActive;
            package.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully updated subscription package {Id}: {PackageName}", 
                id, 
                package.PackageName);

            // Return updated package
            return new SubscriptionPackageDto
            {
                Id = package.Id,
                PackageName = package.PackageName,
                Price = package.Price,
                DurationDays = package.DurationDays,
                Type = package.Type,
                Description = package.Description,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt,
                UpdatedAt = package.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription package with ID: {Id}", id);
            throw;
        }
    }

    public async Task<List<VenueSubscriptionPackageDto>> GetVenueSubscriptionPackagesByVenueIdAsync(int venueId)
    {
        try
        {
            _logger.LogInformation("Getting venue subscription packages for venue ID: {VenueId}", venueId);

            var venueSubscriptions = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Include(vsp => vsp.Package)
                .Where(vsp => vsp.VenueId.HasValue && vsp.VenueId.Value == venueId)
                .OrderByDescending(vsp => vsp.CreatedAt)
                .Select(vsp => new VenueSubscriptionPackageDto
                {
                    Id = vsp.Id,
                    VenueId = vsp.VenueId!.Value,
                    PackageId = vsp.PackageId,
                    StartDate = vsp.StartDate,
                    EndDate = vsp.EndDate,
                    Quantity = vsp.Quantity,
                    Status = vsp.Status,
                    CreatedAt = vsp.CreatedAt,
                    Package = vsp.Package != null ? new SubscriptionPackageDto
                    {
                        Id = vsp.Package.Id,
                        PackageName = vsp.Package.PackageName,
                        Price = vsp.Package.Price,
                        DurationDays = vsp.Package.DurationDays,
                        Type = vsp.Package.Type,
                        Description = vsp.Package.Description,
                        IsActive = vsp.Package.IsActive,
                        CreatedAt = vsp.Package.CreatedAt,
                        UpdatedAt = vsp.Package.UpdatedAt
                    } : null
                })
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} venue subscription packages for venue ID {VenueId}", 
                venueSubscriptions.Count, 
                venueId);

            return venueSubscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue subscription packages for venue ID: {VenueId}", venueId);
            throw;
        }
    }

    public async Task<List<VenueSubscriptionPackageDto>> GetVenueSubscriptionPackagesByOwnerUserIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting venue subscription packages for venue owner user ID: {UserId}", userId);

            // Get venue owner profile by user ID
            var venueOwner = await _unitOfWork.Context.Set<VenueOwnerProfile>()
                .FirstOrDefaultAsync(vo => vo.UserId == userId && vo.IsDeleted != true);

            if (venueOwner == null)
            {
                throw new InvalidOperationException($"Venue owner profile not found for user ID {userId}");
            }

            // Get all venues owned by this venue owner
            var venueIds = await _unitOfWork.Context.Set<VenueLocation>()
                .Where(v => v.VenueOwnerId == venueOwner.Id && v.IsDeleted != true)
                .Select(v => v.Id)
                .ToListAsync();

            if (!venueIds.Any())
            {
                _logger.LogInformation("No venues found for venue owner user ID: {UserId}", userId);
                return new List<VenueSubscriptionPackageDto>();
            }

            // Get all subscription packages for these venues
            var venueSubscriptions = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Include(vsp => vsp.Package)
                .Include(vsp => vsp.Venue)
                .Where(vsp => vsp.VenueId.HasValue && venueIds.Contains(vsp.VenueId.Value))
                .OrderByDescending(vsp => vsp.CreatedAt)
                .Select(vsp => new VenueSubscriptionPackageDto
                {
                    Id = vsp.Id,
                    VenueId = vsp.VenueId!.Value,
                    PackageId = vsp.PackageId,
                    StartDate = vsp.StartDate,
                    EndDate = vsp.EndDate,
                    Quantity = vsp.Quantity,
                    Status = vsp.Status,
                    CreatedAt = vsp.CreatedAt,
                    VenueName = vsp.Venue != null ? vsp.Venue.Name : null,
                    Package = vsp.Package != null ? new SubscriptionPackageDto
                    {
                        Id = vsp.Package.Id,
                        PackageName = vsp.Package.PackageName,
                        Price = vsp.Package.Price,
                        DurationDays = vsp.Package.DurationDays,
                        Type = vsp.Package.Type,
                        Description = vsp.Package.Description,
                        IsActive = vsp.Package.IsActive,
                        CreatedAt = vsp.Package.CreatedAt,
                        UpdatedAt = vsp.Package.UpdatedAt
                    } : null
                })
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} venue subscription packages for venue owner user ID {UserId} across {VenueCount} venues", 
                venueSubscriptions.Count, 
                userId,
                venueIds.Count);

            return venueSubscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue subscription packages for venue owner user ID: {UserId}", userId);
            throw;
        }
    }
}
