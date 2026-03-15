using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class AdvertisementRepository : GenericRepository<Advertisement>, IAdvertisementRepository
{
    public AdvertisementRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<VenueLocationAdvertisement>> GetActiveAdvertisementsAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.VenueLocationAdvertisements
            .Include(vla => vla.Advertisement)
            .Include(vla => vla.Venue)
            .Where(vla =>
                vla.Status == VenueLocationAdvertisementStatus.ACTIVE.ToString() &&
                // vla.StartDate <= now &&
                // vla.EndDate >= now &&
                vla.Advertisement.Status == AdvertisementStatus.APPROVED.ToString() &&
                vla.Advertisement.IsDeleted == false &&
                vla.Venue.IsDeleted == false &&
                vla.Venue.Status == "ACTIVE"
            )
            .OrderByDescending(vla => vla.PriorityScore)
            .ThenByDescending(vla => vla.Advertisement.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Advertisement>> GetByVenueOwnerIdAsync(int venueOwnerId)
    {
        return await _context.Advertisements
            .Include(a => a.VenueLocationAdvertisements)
                .ThenInclude(vla => vla.Venue)
            .Include(a => a.AdsOrders)
                .ThenInclude(ao => ao.Package)
            .Where(a => a.VenueOwnerId == venueOwnerId && a.IsDeleted != true)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Advertisement?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Advertisements
            .Include(a => a.VenueOwner)
            .Include(a => a.VenueLocationAdvertisements)
                .ThenInclude(vla => vla.Venue)
            .Include(a => a.AdsOrders)
                .ThenInclude(ao => ao.Package)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted != true);
    }
}
