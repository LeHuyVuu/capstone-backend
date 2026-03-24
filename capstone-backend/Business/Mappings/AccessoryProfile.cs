using AutoMapper;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class AccessoryProfile : Profile
    {
        public AccessoryProfile()
        {
            CreateMap<Accessory, AccessoryResponse>()
                .ForMember(dest => dest.AccessoryId, opt => opt.MapFrom(src => src.Id));
            CreateMap<Accessory, AccessoryDetailResponse>()
                .ForMember(dest => dest.AccessoryId, opt => opt.MapFrom(src => src.Id));

            CreateMap<MemberAccessory, MyAccessoryResponse>()
                .ForMember(dest => dest.MemberAccessoryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AccessoryId, opt => opt.MapFrom(src => src.AccessoryId))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Accessory.Code))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Accessory.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Accessory.Type))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.Accessory.ThumbnailUrl));
        }
    }
}
