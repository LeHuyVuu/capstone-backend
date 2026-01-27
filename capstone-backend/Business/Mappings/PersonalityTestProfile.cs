using AutoMapper;
using capstone_backend.Business.DTOs.PersonalityTest;
using capstone_backend.Data.Entities;
using System.Text.Json.Nodes;

namespace capstone_backend.Business.Mappings
{
    public class PersonalityTestProfile : Profile
    {
        public PersonalityTestProfile()
        {
            CreateMap<PersonalityTest, PersonalityTestResponse>();
            CreateMap<PersonalityTest, PersonalityTestDetailResponse>();
        }
    }
}
