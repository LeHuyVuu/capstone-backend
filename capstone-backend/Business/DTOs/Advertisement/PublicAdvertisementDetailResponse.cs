namespace capstone_backend.Business.DTOs.Advertisement;

/// <summary>
/// Public advertisement detail with venue information
/// Used when user clicks on an advertisement banner
/// </summary>
public class PublicAdvertisementDetailResponse
{
    // Advertisement Info
    public int AdvertisementId { get; set; }
    public string Title { get; set; } = null!;
    public string? Content { get; set; }
    public string BannerUrl { get; set; } = null!;
    public string? TargetUrl { get; set; }
    public string PlacementType { get; set; } = null!;
    
    // List of Venues (one advertisement can have multiple venues)
    public List<VenueDetailInfo> Venues { get; set; } = new();
}

/// <summary>
/// Venue detail information in advertisement
/// </summary>
public class VenueDetailInfo
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = null!;
    public string? VenueDescription { get; set; }
    public string VenueAddress { get; set; } = null!;
    public string? VenuePhoneNumber { get; set; }
    public string? VenueEmail { get; set; }
    public string? VenueWebsiteUrl { get; set; }
    public decimal? VenuePriceMin { get; set; }
    public decimal? VenuePriceMax { get; set; }
    public decimal? VenueLatitude { get; set; }
    public decimal? VenueLongitude { get; set; }
    public decimal? VenueAverageRating { get; set; }
    public int? VenueReviewCount { get; set; }
    public List<string> VenueCoverImage { get; set; } = new();
    public List<string> VenueInteriorImage { get; set; } = new();
    public List<string> VenueCategory { get; set; } = new();
}
