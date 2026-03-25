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
            CreateMap<Post, PostResponse>();
            CreateMap<MemberProfile, MemberCommentResponse>()
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.User.AvatarUrl));

            // Mutation
            CreateMap<CreatePostRequest, Post>();
            CreateMap<UpdatePostRequest, Post>();

            // Interaction
            CreateMap<Comment, CommentResponse>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
                .ForMember(dest => dest.ReplyToMember, opt => opt.MapFrom(src => src.TargetMember));
        }
    }
}
