using AutoMapper;
using capstone_backend.Business.DTOs.VenueSettlement;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class VenueSettlementProfile : Profile 
    {
        public VenueSettlementProfile()
        {
            CreateMap<VenueSettlement, VenueSettlementListItemResponse>()
                .ForMember(dest => dest.SettlementId, opt => opt.MapFrom(src => src.Id));
        }
    }
}
