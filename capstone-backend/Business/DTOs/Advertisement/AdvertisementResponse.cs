namespace capstone_backend.Business.DTOs.Advertisement;

public class AdvertisementResponse
{
    public string Type { get; set; } = null!; // "ADVERTISEMENT" hoặc "SPECIAL_EVENT"
    public int? AdvertisementId { get; set; } // ID của advertisement (nếu type = ADVERTISEMENT)
    public int? VenueId { get; set; } // ID của venue (nếu type = ADVERTISEMENT)
    public int? SpecialEventId { get; set; } // ID của special event (nếu type = SPECIAL_EVENT)
    public string? BannerUrl { get; set; } // URL ảnh banner
}
