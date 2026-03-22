using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueOwner;

namespace capstone_backend.Business.Interfaces;

public interface IVenueOwnerProfileService
{
    Task<VenueOwnerProfileResponse?> UpdateVenueOwnerProfileAsync(int userId, UpdateVenueOwnerProfileRequest request);
}
