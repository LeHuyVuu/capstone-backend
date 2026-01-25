using AutoMapper;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings;

/// <summary>
/// AutoMapper profile for VenueLocation entity mappings
/// </summary>
public class VenueLocationProfile : Profile
{
    public VenueLocationProfile()
    {
        // VenueLocation entity to VenueLocationDetailResponse
        CreateMap<VenueLocation, VenueLocationDetailResponse>();

        // LocationTag to LocationTagInfo with custom mapping for TagName
        CreateMap<LocationTag, LocationTagInfo>()
            .ForMember(dest => dest.TagName, opt => opt.MapFrom(src => 
                (src.CoupleMoodType != null || src.CouplePersonalityType != null) 
                    ? (src.CoupleMoodType != null && src.CouplePersonalityType != null 
                        ? src.CoupleMoodType.Name + " - " + src.CouplePersonalityType.Name 
                        : src.CoupleMoodType != null 
                            ? src.CoupleMoodType.Name 
                            : (src.CouplePersonalityType != null ? src.CouplePersonalityType.Name : null))
                    : null));

        // CoupleMoodType to CoupleMoodTypeInfo
        CreateMap<CoupleMoodType, CoupleMoodTypeInfo>();

        // CouplePersonalityType to CouplePersonalityTypeInfo
        CreateMap<CouplePersonalityType, CouplePersonalityTypeInfo>();

        // VenueOwnerProfile to VenueOwnerProfileResponse
        CreateMap<VenueOwnerProfile, VenueOwnerProfileResponse>();

        // Review to VenueReviewResponse
        CreateMap<Review, VenueReviewResponse>();
    }

    /// <summary>
    /// Generate tag name by combining couple mood type and couple personality type
    /// </summary>
    private static string? GenerateTagName(string? moodTypeName, string? personalityTypeName)
    {
        if (string.IsNullOrEmpty(moodTypeName) && string.IsNullOrEmpty(personalityTypeName))
            return null;

        if (string.IsNullOrEmpty(moodTypeName))
            return personalityTypeName;

        if (string.IsNullOrEmpty(personalityTypeName))
            return moodTypeName;

        return $"{moodTypeName} - {personalityTypeName}";
    }
}
