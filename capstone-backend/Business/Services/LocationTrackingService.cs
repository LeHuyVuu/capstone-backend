using capstone_backend.Business.DTOs.Location;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace capstone_backend.Business.Services
{
    public class LocationTrackingService : ILocationTrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly ConcurrentDictionary<int, (LocationUpdateDto Location, DateTime LastUpdate)> MemberLocations = new();

        public LocationTrackingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool IsValid, int? CoupleId, int? PartnerId)> ValidateCoupleAccessAsync(int memberId)
        {
            var activeCouple = await _unitOfWork.CoupleProfiles
                .GetActiveCoupleByMemberIdAsync(memberId);

            if (activeCouple == null)
                return (false, null, null);

            int partnerId = activeCouple.MemberId1 == memberId 
                ? activeCouple.MemberId2 
                : activeCouple.MemberId1;

            return (true, activeCouple.id, partnerId);
        }

        public async Task<LocationSharingStatusDto> GetLocationSharingStatusAsync(int memberId)
        {
            var (isValid, coupleId, partnerId) = await ValidateCoupleAccessAsync(memberId);

            if (!isValid || !partnerId.HasValue)
            {
                return new LocationSharingStatusDto
                {
                    IsEnabled = false,
                    CoupleId = null,
                    PartnerId = null,
                    PartnerName = null,
                    PartnerIsOnline = false
                };
            }

            var partner = await _unitOfWork.Context.MemberProfiles
                .FirstOrDefaultAsync(m => m.Id == partnerId.Value);

            var partnerLocation = GetPartnerLocation(memberId, partnerId.Value);

            return new LocationSharingStatusDto
            {
                IsEnabled = true,
                CoupleId = coupleId,
                PartnerId = partnerId,
                PartnerName = partner?.FullName,
                PartnerIsOnline = partnerLocation != null && partnerLocation.IsOnline
            };
        }

        public void UpdateMemberLocation(int memberId, LocationUpdateDto location)
        {
            if (!IsValidCoordinates(location.Latitude, location.Longitude))
                return;

            MemberLocations.AddOrUpdate(
                memberId,
                (location, DateTime.UtcNow),
                (key, existing) => (location, DateTime.UtcNow)
            );
        }

        public PartnerLocationDto? GetPartnerLocation(int memberId, int partnerId)
        {
            if (!MemberLocations.TryGetValue(partnerId, out var partnerData))
                return null;

            if (!IsLocationRecent(partnerData.LastUpdate))
                return null;

            var partner = _unitOfWork.Context.MemberProfiles
                .FirstOrDefault(m => m.Id == partnerId);

            return new PartnerLocationDto
            {
                PartnerId = partnerId,
                PartnerName = partner?.FullName,
                Latitude = partnerData.Location.Latitude,
                Longitude = partnerData.Location.Longitude,
                Accuracy = partnerData.Location.Accuracy,
                Heading = partnerData.Location.Heading,
                Speed = partnerData.Location.Speed,
                Timestamp = partnerData.Location.Timestamp,
                IsOnline = true
            };
        }

        public void RemoveMemberLocation(int memberId)
        {
            MemberLocations.TryRemove(memberId, out _);
        }

        public bool IsLocationRecent(DateTime timestamp, int maxAgeSeconds = 30)
        {
            return (DateTime.UtcNow - timestamp).TotalSeconds <= maxAgeSeconds;
        }

        private bool IsValidCoordinates(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }
    }
}
