using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueOwner;
using capstone_backend.Business.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class VenueOwnerProfileService : IVenueOwnerProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public VenueOwnerProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VenueOwnerProfileResponse?> UpdateVenueOwnerProfileAsync(int userId, UpdateVenueOwnerProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        if (!string.Equals(user.Role, "VENUEOWNER", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only venue owners can update this profile.");
        }

        var venueOwnerProfile = await _unitOfWork.Context.Set<Data.Entities.VenueOwnerProfile>()
            .FirstOrDefaultAsync(v => v.UserId == userId && v.IsDeleted != true);

        if (venueOwnerProfile == null) return null;

        if (!string.IsNullOrWhiteSpace(request.BusinessName))
            venueOwnerProfile.BusinessName = request.BusinessName;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            venueOwnerProfile.PhoneNumber = request.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(request.Email))
            venueOwnerProfile.Email = request.Email;

        venueOwnerProfile.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return new VenueOwnerProfileResponse
        {
            Id = venueOwnerProfile.Id,
            BusinessName = venueOwnerProfile.BusinessName,
            PhoneNumber = venueOwnerProfile.PhoneNumber,
            Email = venueOwnerProfile.Email,
            Address = venueOwnerProfile.Address,
            CitizenIdFrontUrl = user.CitizenIdFrontUrl,
            CitizenIdBackUrl = user.CitizenIdBackUrl,
            BusinessLicenseUrl = user.BusinessLicenseUrl
        };
    }
}
