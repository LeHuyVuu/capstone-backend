using AutoMapper;
using capstone_backend.Business.DTOs.CoupleMoodType;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class CoupleMoodTypeProfile : Profile
    {
        public CoupleMoodTypeProfile()
        {
            CreateMap<CoupleMoodType, CoupleMoodTypeResponse>();
        }
    }
}
