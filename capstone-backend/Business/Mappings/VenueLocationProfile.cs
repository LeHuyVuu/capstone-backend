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
                // Map và gom nhóm LocationTags thành CoupleMoodTypes và CouplePersonalityTypes riêng biệt
                if (src.VenueLocationTags != null && src.VenueLocationTags.Any())
                {
                    var locationTags = src.VenueLocationTags
                        .Where(vlt => vlt.LocationTag != null && vlt.IsDeleted != true)
                        .Select(vlt => vlt.LocationTag!)
                        .ToList();

                    // Gom nhóm CoupleMoodTypes (loại bỏ trùng lặp)
                    dest.CoupleMoodTypes = locationTags
                        .Where(lt => lt.CoupleMoodType != null)
                        .Select(lt => lt.CoupleMoodType!)
                        .GroupBy(mt => mt.Id)
                        .Select(g => g.First())
                        .Select(mt => new CoupleMoodTypeInfo
                        {
                            Id = mt.Id,
                            Name = mt.Name,
                        })
                        .ToList();

                    // Gom nhóm CouplePersonalityTypes (loại bỏ trùng lặp)
                    dest.CouplePersonalityTypes = locationTags
                        .Where(lt => lt.CouplePersonalityType != null)
                        .Select(lt => lt.CouplePersonalityType!)
                        .GroupBy(pt => pt.Id)
                        .Select(g => g.First())
                        .Select(pt => new CouplePersonalityTypeInfo
                        {
                            Id = pt.Id,
                            Name = pt.Name,
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
        // Ignore ImageUrls và MatchedTag - xử lý thủ công trong service
        CreateMap<Review, VenueReviewResponse>()
            .ForMember(dest => dest.ImageUrls, opt => opt.Ignore())
            .ForMember(dest => dest.MatchedTag, opt => opt.Ignore());

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
