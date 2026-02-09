using AutoMapper;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<CreateReviewRequest, Review>();
        }
    }
}
