using AutoMapper;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Data.Entities;
using capstone_backend.Extensions.Common;

namespace capstone_backend.Business.Mappings
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<CreateReviewRequest, Review>();
            CreateMap<ReviewReply, ReviewReplyResponse>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
                    src.CreatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.CreatedAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src =>
                    src.UpdatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.UpdatedAt.Value)
                        : (DateTime?)null));
        }
    }
}
