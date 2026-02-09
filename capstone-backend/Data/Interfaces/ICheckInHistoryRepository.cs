using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface ICheckInHistoryRepository : IGenericRepository<CheckInHistory>
    {
        Task<CheckInHistory?> GetLatestByMemberIdAndVenueIdAsync(int memberId, int venueId);
    }
}
