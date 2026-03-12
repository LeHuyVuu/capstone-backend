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

            // Voucher Items
            CreateMap<VoucherItem, VoucherItemResponse>()
                .ForMember(dest => dest.IsAssigned, opt => opt.MapFrom(src => src.VoucherItemMemberId != null));
            CreateMap<VoucherItem, VoucherItemDetailResponse>()
                .ForMember(dest => dest.IsAssigned, opt => opt.MapFrom(src => src.VoucherItemMemberId != null))
                .ForMember(dest => dest.Member,
                    opt => opt.MapFrom(src => src.VoucherItemMemberId != null && src.VoucherItemMember != null
                        ? new VoucherItemMemberBriefResponse
                        {
                            MemberId = src.VoucherItemMemberId.Value,
                            FullName = src.VoucherItemMember.Member != null
                                ? src.VoucherItemMember.Member.FullName
                                : null,
                            AvatarUrl = src.VoucherItemMember.Member != null && src.VoucherItemMember.Member.User != null
                                ? src.VoucherItemMember.Member.User.AvatarUrl
                                : null
                        }
                        : null));

            CreateMap<VoucherItem, VoucherItemValidationAndRedemptionResponse>()
                .ForMember(dest => dest.VoucherTitle, opt => opt.MapFrom(src => src.Voucher.Title))
                .ForMember(dest => dest.VoucherDescription, opt => opt.MapFrom(src => src.Voucher.Description))
                .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.Voucher.DiscountType))
                .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.Voucher.DiscountAmount))
                .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => src.Voucher.DiscountPercent))
                .ForMember(dest => dest.Member,
                    opt => opt.MapFrom(src => src.VoucherItemMemberId != null && src.VoucherItemMember != null
                        ? new VoucherItemMemberBriefResponse
                        {
                            MemberId = src.VoucherItemMemberId.Value,
                            FullName = src.VoucherItemMember.Member != null
                                ? src.VoucherItemMember.Member.FullName
                                : null,
                            AvatarUrl = src.VoucherItemMember.Member != null && src.VoucherItemMember.Member.User != null
                                ? src.VoucherItemMember.Member.User.AvatarUrl
                                : null
                        }
                        : null));

            CreateMap<Voucher, MemberVoucherListItemResponse>()
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.VoucherLocations.Select(vl => new MemberVoucherLocationItemResponse
                {
                    VenueLocationId = vl.VenueLocationId,
                    VenueLocationName = vl.VenueLocation.Name
                }).ToList()));

            CreateMap<Voucher, MemberVoucherDetailResponse>()
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.VoucherLocations.Select(vl => new MemberVoucherLocationItemResponse
                {
                    VenueLocationId = vl.VenueLocationId,
                    VenueLocationName = vl.VenueLocation.Name
                }).ToList()));

            CreateMap<VoucherItem, ExchangeVoucherItemResult>()
                .ForMember(dest => dest.VoucherTitle, opt => opt.MapFrom(src => src.Voucher.Title));
        }
    }
}
