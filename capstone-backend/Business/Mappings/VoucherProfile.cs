using AutoMapper;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class VoucherProfile : Profile
    {
        public VoucherProfile()
        {
            CreateMap<CreateVoucherRequest, Voucher>();
            CreateMap<Voucher, VoucherResponse>();
            CreateMap<UpdateVoucherRequest, Voucher>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Voucher, VoucherDetailResponse>()
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.VoucherLocations.Select(vl => new VoucherLocationItemResponse
                {
                    VenueLocationId = vl.VenueLocationId,
                    VenueLocationName = vl.VenueLocation.Name
                }).ToList()));

            CreateMap<Voucher, AdminVoucherDetailResponse>()
                .ForMember(dest => dest.VenueOwnerName, opt => opt.MapFrom(src => src.VenueOwner != null ? src.VenueOwner.BusinessName : null))
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.VoucherLocations.Select(vl => new VoucherLocationItemResponse
                {
                    VenueLocationId = vl.VenueLocationId,
                    VenueLocationName = vl.VenueLocation.Name
                }).ToList()));

            CreateMap<Voucher, VoucherSummaryResponse>();
        }
    }
}
