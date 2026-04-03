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
                .ForMember(dest => dest.SettlementId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VoucherItemCode, opt => opt.MapFrom(src => src.VoucherItem.ItemCode))
                .ForMember(dest => dest.VoucherTitle, opt => opt.MapFrom(src => src.VoucherItem.Voucher.Title))
                .ForMember(dest => dest.UsedAt, opt => opt.MapFrom(src => src.VoucherItem.UsedAt));
            CreateMap<VenueSettlement, VenueSettlementDetailResponse>()
                .ForMember(dest => dest.SettlementId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VoucherItemCode, opt => opt.MapFrom(src => src.VoucherItem.ItemCode))
                .ForMember(dest => dest.UsedAt, opt => opt.MapFrom(src => src.VoucherItem.UsedAt))
                .ForMember(dest => dest.VoucherTitle, opt => opt.MapFrom(src => src.VoucherItem.Voucher.Title))
                .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.VoucherItemMember.Member.FullName));
        }
    }
}
