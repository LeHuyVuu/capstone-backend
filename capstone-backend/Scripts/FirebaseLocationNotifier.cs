using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Concurrent;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Entities;
using Hangfire;

namespace capstone_backend.Scripts;

public sealed class FirebaseLocationNotifier
{
    private static readonly HashSet<int> PremiumMemberPackageIds = new() { 6, 7 };
    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MovementDecisionEngine _movementEngine;
    private readonly ConcurrentDictionary<string, bool> _seededKeys = new();

    public FirebaseLocationNotifier(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        FirebaseLocationNotifierOptions? options = null)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _movementEngine = new MovementDecisionEngine(options ?? new FirebaseLocationNotifierOptions());
    }

    // Listen one couple path: /locations/{coupleId}.json
    public async Task ListenCoupleAsync(string firebaseBaseUrl, int coupleId, string? authToken, CancellationToken cancellationToken)
    {
        var baseUrl = firebaseBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/locations/{coupleId}.json";
        if (!string.IsNullOrWhiteSpace(authToken))
            url += $"?auth={Uri.EscapeDataString(authToken)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        string? eventData = null;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) continue;

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                eventType = line[6..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                eventData = line[5..].Trim();
                continue;
            }

            if (line.Length == 0)
            {
                await HandleSseEventAsync(coupleId, eventType, eventData, cancellationToken);
                eventType = null;
                eventData = null;
            }
        }
    }

    private async Task HandleSseEventAsync(int coupleId, string? eventType, string? eventData, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(eventData))
            return;

        if (eventType.Equals("keep-alive", StringComparison.OrdinalIgnoreCase) || eventData == "null")
            return;

        if (!eventType.Equals("put", StringComparison.OrdinalIgnoreCase)
            && !eventType.Equals("patch", StringComparison.OrdinalIgnoreCase))
            return;

        using var doc = JsonDocument.Parse(eventData);

        if (!doc.RootElement.TryGetProperty("path", out var pathEl)
            || !doc.RootElement.TryGetProperty("data", out var dataEl))
            return;

        var path = pathEl.GetString() ?? string.Empty;

        // Initial snapshot for whole node: path="/" and data={ "10": {...}, "20": {...} }
        if (path == "/" && dataEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var child in dataEl.EnumerateObject())
            {
                if (int.TryParse(child.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var memberId))
                    await HandleMemberLocationAsync(coupleId, memberId, child.Value, isInitialSnapshot: true, cancellationToken);
            }
            return;
        }

        // Single member changed: path="/10"
        if (TryParseMemberId(path, out var changedMemberId))
            await HandleMemberLocationAsync(coupleId, changedMemberId, dataEl, isInitialSnapshot: false, cancellationToken);
    }

    private async Task HandleMemberLocationAsync(
        int coupleId,
        int changedMemberId,
        JsonElement locationJson,
        bool isInitialSnapshot,
        CancellationToken cancellationToken)
    {
        if (!TryGetLocation(locationJson, out var lat, out var lng, out var updatedAt))
            return;

        var sample = new LocationSample(lat, lng, updatedAt);
        var key = BuildKey(coupleId, changedMemberId);

        // First snapshot value: save only once, do not notify.
        if (isInitialSnapshot && _seededKeys.TryAdd(key, true))
        {
            _movementEngine.Seed(key, sample);
            return;
        }

        var activeSubscription = await _unitOfWork.MemberSubscriptionPackages
            .GetCurrentActiveSubscriptionAsync(changedMemberId);
        if (activeSubscription == null || !PremiumMemberPackageIds.Contains(activeSubscription.PackageId))
            return;

        if (!_movementEngine.ShouldNotify(key, sample))
            return;

        var targetUserId = await ResolvePartnerUserIdAsync(coupleId, changedMemberId);
        if (!targetUserId.HasValue)
            return;

        var notification = new Notification
        {
            UserId = targetUserId.Value,
            Title = "Có vị trí mới từ người ấy 📍",
            Message = "Nhấn để xem vị trí mới nhất của đối phương.",
            Type = NotificationType.MAP.ToString(),
            ReferenceId = coupleId,
            ReferenceType = "MAP",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        // Follow existing project pattern: push worker will resolve device tokens and send FCM.
        BackgroundJob.Enqueue<INotificationWorker>(job => job.SendPushNotificationAsync(notification.Id));
    }

    private async Task<int?> ResolvePartnerUserIdAsync(int coupleId, int changedMemberId)
    {
        var couple = await _unitOfWork.CoupleProfiles.GetByIdAsync(coupleId);
        if (couple == null)
            return null;

        if (couple.IsDeleted == true)
            return null;

        if (!string.Equals(couple.Status, CoupleProfileStatus.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase))
            return null;

        if (changedMemberId != couple.MemberId1 && changedMemberId != couple.MemberId2)
            return null;

        var partnerMemberId = couple.MemberId1 == changedMemberId ? couple.MemberId2 : couple.MemberId1;

        var partnerMember = await _unitOfWork.MembersProfile.GetByIdAsync(partnerMemberId);
        if (partnerMember == null || partnerMember.IsDeleted == true)
            return null;

        var partnerUser = await _unitOfWork.Users.GetByIdAsync(partnerMember.UserId);
        if (partnerUser == null || partnerUser.IsDeleted == true || partnerUser.IsActive != true)
            return null;

        return partnerUser.Id;
    }

    private static bool TryParseMemberId(string path, out int memberId)
    {
        memberId = 0;
        var trimmed = path.Trim('/');
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var firstSegment = trimmed.Split('/')[0];
        return int.TryParse(firstSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out memberId);
    }

    private static bool TryGetLocation(JsonElement json, out double lat, out double lng, out long updatedAt)
    {
        lat = 0;
        lng = 0;
        updatedAt = 0;

        if (json.ValueKind != JsonValueKind.Object)
            return false;

        if (!json.TryGetProperty("lat", out var latEl)
            || !json.TryGetProperty("lng", out var lngEl)
            || !json.TryGetProperty("updatedAt", out var updatedAtEl))
            return false;

        if (!latEl.TryGetDouble(out lat) || !lngEl.TryGetDouble(out lng) || !updatedAtEl.TryGetInt64(out updatedAt))
            return false;

        return true;
    }

    private static string BuildKey(int coupleId, int memberId) => $"{coupleId}:{memberId}";
}

/*
Minimal usage:

var httpClient = new HttpClient();
var listener = new FirebaseLocationNotifier(
    httpClient,
    unitOfWork);

await listener.ListenCoupleAsync(
    "https://couplemood-firebase-default-rtdb.asia-southeast1.firebasedatabase.app",
    31,
    authToken: null,
    cancellationToken);
*/
