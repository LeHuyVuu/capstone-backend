using capstone_backend.Business.DTOs.Location;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace capstone_backend.Hubs
{
    [Authorize]
    public class LocationTrackingHub : Hub
    {
        private readonly ILocationTrackingService _locationService;
        private readonly IUnitOfWork _unitOfWork;
        
        private static readonly ConcurrentDictionary<int, HashSet<string>> MemberConnections = new();
        private static readonly ConcurrentDictionary<int, DateTime> LastLocationUpdate = new();
        
        private const int MinUpdateIntervalSeconds = 2;

        public LocationTrackingHub(
            ILocationTrackingService locationService,
            IUnitOfWork unitOfWork)
        {
            _locationService = locationService;
            _unitOfWork = unitOfWork;
        }

        public override async Task OnConnectedAsync()
        {
            var memberId = await GetCurrentMemberIdAsync();
            if (!memberId.HasValue)
            {
                Context.Abort();
                return;
            }

            var (hasCouple, coupleId, partnerId) = await _locationService.ValidateCoupleAccessAsync(memberId.Value);
            
            if (!hasCouple || !coupleId.HasValue || !partnerId.HasValue)
            {
                await Clients.Caller.SendAsync("LocationSharingDisabled", "Bạn chưa có couple hoặc couple chưa active");
                return;
            }

            TrackConnectionForMember(memberId.Value);
            await JoinCoupleGroupAsync(coupleId.Value);
            await SendCurrentStatusToUser(memberId.Value);
            await SendPartnerLocationIfAvailable(memberId.Value, partnerId.Value);
            await NotifyPartnerUserIsOnline(memberId.Value, partnerId.Value);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var memberId = await GetCurrentMemberIdAsync();
            if (!memberId.HasValue)
                return;

            RemoveConnectionForMember(memberId.Value);

            if (!IsUserStillConnected(memberId.Value))
            {
                _locationService.RemoveMemberLocation(memberId.Value);
                LastLocationUpdate.TryRemove(memberId.Value, out _);

                var (hasCouple, _, partnerId) = await _locationService.ValidateCoupleAccessAsync(memberId.Value);
                if (hasCouple && partnerId.HasValue)
                {
                    await NotifyPartnerUserIsOffline(memberId.Value, partnerId.Value);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task UpdateLocation(LocationUpdateDto locationUpdate)
        {
            var memberId = await GetCurrentMemberIdAsync();
            if (!memberId.HasValue)
                return;

            if (!CanUpdateLocation(memberId.Value))
                return;

            var (hasCouple, _, partnerId) = await _locationService.ValidateCoupleAccessAsync(memberId.Value);
            if (!hasCouple || !partnerId.HasValue)
            {
                await Clients.Caller.SendAsync("LocationSharingDisabled", "Couple không còn active");
                return;
            }

            if (!IsValidLocation(locationUpdate))
                return;

            _locationService.UpdateMemberLocation(memberId.Value, locationUpdate);
            LastLocationUpdate[memberId.Value] = DateTime.UtcNow;

            await SendLocationToPartner(memberId.Value, partnerId.Value, locationUpdate);
        }

        public async Task RequestPartnerLocation()
        {
            var memberId = await GetCurrentMemberIdAsync();
            if (!memberId.HasValue)
                return;

            var (hasCouple, _, partnerId) = await _locationService.ValidateCoupleAccessAsync(memberId.Value);
            if (!hasCouple || !partnerId.HasValue)
                return;

            var partnerLocation = _locationService.GetPartnerLocation(memberId.Value, partnerId.Value);
            
            if (partnerLocation != null)
            {
                await Clients.Caller.SendAsync("PartnerLocationUpdate", partnerLocation);
            }
            else
            {
                await Clients.Caller.SendAsync("PartnerLocationUnavailable", new { PartnerId = partnerId.Value });
            }
        }

        public async Task StopSharingLocation()
        {
            var memberId = await GetCurrentMemberIdAsync();
            if (!memberId.HasValue)
                return;

            _locationService.RemoveMemberLocation(memberId.Value);

            var (hasCouple, _, partnerId) = await _locationService.ValidateCoupleAccessAsync(memberId.Value);
            if (hasCouple && partnerId.HasValue)
            {
                await NotifyPartner(partnerId.Value, "PartnerStoppedSharing", new { MemberId = memberId.Value });
            }
        }

        private async Task<int?> GetCurrentMemberIdAsync()
        {
            var userIdClaim = Context.User?.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "userId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            var memberProfile = await _unitOfWork.Context.MemberProfiles
                .FirstOrDefaultAsync(m => m.UserId == userId);

            return memberProfile?.Id;
        }

        private void TrackConnectionForMember(int memberId)
        {
            MemberConnections.AddOrUpdate(
                memberId,
                new HashSet<string> { Context.ConnectionId },
                (key, set) =>
                {
                    set.Add(Context.ConnectionId);
                    return set;
                });
        }

        private void RemoveConnectionForMember(int memberId)
        {
            if (MemberConnections.TryGetValue(memberId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    MemberConnections.TryRemove(memberId, out _);
                }
            }
        }

        private bool IsUserStillConnected(int memberId)
        {
            return MemberConnections.ContainsKey(memberId);
        }

        private async Task JoinCoupleGroupAsync(int coupleId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"couple_{coupleId}");
        }

        private async Task SendCurrentStatusToUser(int memberId)
        {
            var status = await _locationService.GetLocationSharingStatusAsync(memberId);
            await Clients.Caller.SendAsync("LocationSharingStatus", status);
        }

        private async Task SendPartnerLocationIfAvailable(int memberId, int partnerId)
        {
            var partnerLocation = _locationService.GetPartnerLocation(memberId, partnerId);
            if (partnerLocation != null)
            {
                await Clients.Caller.SendAsync("PartnerLocationUpdate", partnerLocation);
            }
        }

        private async Task NotifyPartnerUserIsOnline(int memberId, int partnerId)
        {
            if (IsUserStillConnected(partnerId))
            {
                await NotifyPartner(partnerId, "PartnerOnline", new { MemberId = memberId });
            }
        }

        private async Task NotifyPartnerUserIsOffline(int memberId, int partnerId)
        {
            await NotifyPartner(partnerId, "PartnerOffline", new { MemberId = memberId });
        }

        private async Task SendLocationToPartner(int memberId, int partnerId, LocationUpdateDto locationUpdate)
        {
            var partnerLocation = new PartnerLocationDto
            {
                PartnerId = memberId,
                Latitude = locationUpdate.Latitude,
                Longitude = locationUpdate.Longitude,
                Accuracy = locationUpdate.Accuracy,
                Heading = locationUpdate.Heading,
                Speed = locationUpdate.Speed,
                Timestamp = locationUpdate.Timestamp,
                IsOnline = true
            };

            await NotifyPartner(partnerId, "PartnerLocationUpdate", partnerLocation);
        }

        private async Task NotifyPartner(int partnerId, string eventName, object data)
        {
            if (MemberConnections.TryGetValue(partnerId, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    await Clients.Client(connectionId).SendAsync(eventName, data);
                }
            }
        }

        private bool CanUpdateLocation(int memberId)
        {
            if (LastLocationUpdate.TryGetValue(memberId, out var lastUpdate))
            {
                var timeSinceLastUpdate = (DateTime.UtcNow - lastUpdate).TotalSeconds;
                return timeSinceLastUpdate >= MinUpdateIntervalSeconds;
            }
            return true;
        }

        private bool IsValidLocation(LocationUpdateDto location)
        {
            if (location.Latitude == 0 && location.Longitude == 0)
                return false;

            if (Math.Abs(location.Latitude) > 90 || Math.Abs(location.Longitude) > 180)
                return false;

            return true;
        }
    }
}
