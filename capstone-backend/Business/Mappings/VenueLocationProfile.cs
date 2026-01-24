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

        // LocationTag to LocationTagInfo
        CreateMap<LocationTag, LocationTagInfo>();

        // CoupleMoodType to CoupleMoodTypeInfo
        CreateMap<CoupleMoodType, CoupleMoodTypeInfo>();

        // CouplePersonalityType to CouplePersonalityTypeInfo
        CreateMap<CouplePersonalityType, CouplePersonalityTypeInfo>();

        // VenueOwnerProfile to VenueOwnerProfileResponse
        CreateMap<VenueOwnerProfile, VenueOwnerProfileResponse>();
    }
}
