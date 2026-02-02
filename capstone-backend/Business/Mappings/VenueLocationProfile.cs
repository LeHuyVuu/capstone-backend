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
        // Ignore image fields - handled manually with JSON deserialize
        CreateMap<VenueLocation, VenueLocationDetailResponse>()
            .ForMember(dest => dest.CoverImage, opt => opt.Ignore())
            .ForMember(dest => dest.InteriorImage, opt => opt.Ignore())
            .ForMember(dest => dest.FullPageMenuImage, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                // Map ALL LocationTags from VenueLocationTags collection (many-to-many)
                if (src.VenueLocationTags != null && src.VenueLocationTags.Any())
                {
                    dest.LocationTags = src.VenueLocationTags
                        .Where(vlt => vlt.LocationTag != null && vlt.IsDeleted != true)
                        .Select(vlt => new LocationTagInfo
                        {
                            Id = vlt.LocationTag!.Id,
                            TagName = GenerateTagName(vlt.LocationTag.CoupleMoodType?.Name, vlt.LocationTag.CouplePersonalityType?.Name),
                            CoupleMoodType = vlt.LocationTag.CoupleMoodType != null ? new CoupleMoodTypeInfo
                            {
                                Id = vlt.LocationTag.CoupleMoodType.Id,
                                Name = vlt.LocationTag.CoupleMoodType.Name,
                                Description = vlt.LocationTag.CoupleMoodType.Description,
                                IsActive = vlt.LocationTag.CoupleMoodType.IsActive
                            } : null,
                            CouplePersonalityType = vlt.LocationTag.CouplePersonalityType != null ? new CouplePersonalityTypeInfo
                            {
                                Id = vlt.LocationTag.CouplePersonalityType.Id,
                                Name = vlt.LocationTag.CouplePersonalityType.Name,
                                Description = vlt.LocationTag.CouplePersonalityType.Description,
                                IsActive = vlt.LocationTag.CouplePersonalityType.IsActive
                            } : null
                        })
                        .ToList();
                }
            });

        // VenueLocation entity to VenueLocationCreateResponse
        CreateMap<VenueLocation, VenueLocationCreateResponse>();

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

        // MemberProfile to ReviewMemberInfo
        CreateMap<MemberProfile, ReviewMemberInfo>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : null))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null));

        // VenueOpeningHour to TodayOpeningHourResponse
        CreateMap<VenueOpeningHour, TodayOpeningHourResponse>();
    }

    /// <summary>
    /// Generate tag name by combining couple mood type and couple personality type
    /// </summary>
    public static string? GenerateTagName(string? moodTypeName, string? personalityTypeName)
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
