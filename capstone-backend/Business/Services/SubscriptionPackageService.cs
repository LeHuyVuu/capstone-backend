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
            if (normalizedType != "MEMBER" && normalizedType != "VENUE")
            {
                throw new ArgumentException("Type must be either MEMBER or VENUE", nameof(type));
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
}
