using AutoMapper;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Data.Entities;
using capstone_backend.Extensions.Common;
using static capstone_backend.Business.Services.VenueLocationService;

namespace capstone_backend.Business.Mappings
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<CreateReviewRequest, Review>();

            // Review reply
            CreateMap<ReviewReplyRequest, ReviewReply>();
            CreateMap<ReviewReply, ReviewReplyResponse>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
                    src.CreatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.CreatedAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src =>
                    src.UpdatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.UpdatedAt.Value)
                        : (DateTime?)null));

            CreateMap<Review, MyReviewResponse>()
                .ForMember(dest => dest.VenueId, opt => opt.MapFrom(src => src.VenueId))
                .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue.Name))
                .ForMember(dest => dest.VenueCoverImage, opt => opt.MapFrom(src => DeserializeImages(src.Venue.CoverImage)));

            CreateMap<VenueLocation, ReviewVenueInfo>()
                .ForMember(dest => dest.VenueId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.VenueCoverImage, opt => opt.MapFrom(src => DeserializeImages(src.CoverImage)));
            CreateMap<Review, ReviewResponse>();
        }
    }
}
