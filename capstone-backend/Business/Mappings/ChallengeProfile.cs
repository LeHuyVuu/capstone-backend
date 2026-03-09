using AutoMapper;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class ChallengeProfile : Profile
    {
        public ChallengeProfile()
        {
            CreateMap<CreateChallengeRequest, Challenge>();
            CreateMap<UpdateChallengeRequest, Challenge>();
            CreateMap<Challenge, ChallengeResponse>();

            // Couple challenge
            CreateMap<CoupleProfileChallenge, CoupleChallengeListItemResponse>();
            CreateMap<CoupleProfileChallenge, CoupleChallengeDetailResponse>();
        }
    }
}
