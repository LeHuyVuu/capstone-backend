using AutoMapper;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class PostProfile : Profile 
    {
        public PostProfile()
        {
            CreateMap<Post, PostFeedResponse>();
            CreateMap<MemberProfile, AuthorResponse>();
        }
    }
}
