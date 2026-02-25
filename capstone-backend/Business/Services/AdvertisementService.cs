using capstone_backend.Business.DTOs.Advertisement;
using capstone_backend.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class AdvertisementService : IAdvertisementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdvertisementService> _logger;
    private static int _rotationIndex = 0;
    private static readonly object _lock = new object();

    public AdvertisementService(IUnitOfWork unitOfWork, ILogger<AdvertisementService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<AdvertisementResponse>> GetRotatingAdvertisementsAsync(string? placementType = null)
    {
        // Lấy quảng cáo active
        var venueLocationAds = await _unitOfWork.Advertisements.GetActiveAdvertisementsAsync();

        _logger.LogInformation("Total active ads before filter: {Count}", venueLocationAds.Count);

        // Filter by placement type nếu có (case-insensitive)
        if (!string.IsNullOrEmpty(placementType))
        {
            var normalizedPlacementType = placementType.Trim().ToUpper();
            venueLocationAds = venueLocationAds
                .Where(vla => !string.IsNullOrEmpty(vla.Advertisement.PlacementType) && 
                             vla.Advertisement.PlacementType.Trim().ToUpper() == normalizedPlacementType)
                .ToList();
            
            _logger.LogInformation("Filtered ads by PlacementType '{PlacementType}': {Count} ads found", 
                placementType, venueLocationAds.Count);
        }

        // Lấy special events active
        var specialEvents = await _unitOfWork.SpecialEvents.GetActiveSpecialEventsAsync();

        // Nhóm quảng cáo theo priority score
        var groupedByPriority = venueLocationAds
            .GroupBy(vla => vla.PriorityScore ?? 0)
            .OrderByDescending(g => g.Key)
            .ToList();

        var rotatedAds = new List<AdvertisementResponse>();

        // Xoay vòng quảng cáo trong từng nhóm priority
        foreach (var group in groupedByPriority)
        {
            var adsInGroup = group.ToList();
            
            if (adsInGroup.Count > 0)
            {
                // Xoay vòng: lấy index hiện tại, sau đó tăng lên
                lock (_lock)
                {
                    var startIndex = _rotationIndex % adsInGroup.Count;
                    
                    // Sắp xếp lại danh sách bắt đầu từ startIndex
                    var rotated = adsInGroup.Skip(startIndex)
                        .Concat(adsInGroup.Take(startIndex))
                        .ToList();

                    foreach (var vla in rotated)
                    {
                        rotatedAds.Add(new AdvertisementResponse
                        {
                            Type = "ADVERTISEMENT",
                            AdvertisementId = vla.Advertisement.Id,
                            VenueId = vla.Venue.Id,
                            SpecialEventId = null,
                            BannerUrl = vla.Advertisement.BannerUrl,
                        });
                    }

                    // Tăng rotation index cho lần gọi tiếp theo
                    _rotationIndex++;
                }
            }
        }

        // Trộn special events vào (priority thấp hơn, đặt ở cuối)
        var specialEventResponses = specialEvents.Select(se => new AdvertisementResponse
        {
            Type = "SPECIAL_EVENT",
            AdvertisementId = null,
            VenueId = null,
            SpecialEventId = se.Id,
            BannerUrl = se.BannerUrl, // TODO: Thêm field banner_url vào bảng special_events nếu cần
        }).ToList();

        // Kết hợp: Quảng cáo trước (priority cao), special events sau
        var result = rotatedAds.Concat(specialEventResponses).ToList();

        _logger.LogInformation(
            "Retrieved {TotalCount} items: {AdCount} advertisement(s) + {SpecialEventCount} special event(s) (PlacementType: {PlacementType}, RotationIndex: {RotationIndex})",
            result.Count, rotatedAds.Count, specialEventResponses.Count, placementType ?? "all", _rotationIndex);

        return result;
    }
}
