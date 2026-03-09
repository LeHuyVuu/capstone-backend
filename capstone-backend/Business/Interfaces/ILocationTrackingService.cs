using capstone_backend.Business.DTOs.Location;

namespace capstone_backend.Business.Interfaces
{
    public interface ILocationTrackingService
    {
        Task<(bool IsValid, int? CoupleId, int? PartnerId)> ValidateCoupleAccessAsync(int memberId);
        Task<LocationSharingStatusDto> GetLocationSharingStatusAsync(int memberId);
        void UpdateMemberLocation(int memberId, LocationUpdateDto location);
        PartnerLocationDto? GetPartnerLocation(int memberId, int partnerId);
        void RemoveMemberLocation(int memberId);
        bool IsLocationRecent(DateTime timestamp, int maxAgeSeconds = 30);
    }
}
