using AutoMapper;
using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class TestTypeProfile : Profile
    {
        public TestTypeProfile()
        {
            CreateMap<CreateTestTypeResquest, TestType>();
        }
    }
}
