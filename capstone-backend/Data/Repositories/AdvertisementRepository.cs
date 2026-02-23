using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
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
                vla.Status == "ACTIVE" &&
                // vla.StartDate <= now &&
                // vla.EndDate >= now &&
                vla.Advertisement.Status == "APPROVED" &&
                vla.Advertisement.IsDeleted == false &&
                vla.Venue.IsDeleted == false &&
                vla.Venue.Status == "ACTIVE"
            )
            .OrderByDescending(vla => vla.PriorityScore)
            .ThenByDescending(vla => vla.Advertisement.CreatedAt)
            .ToListAsync();
    }
}
