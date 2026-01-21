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
            CreateMap<UpdateTestTypeRequest, TestType>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));   
        }
    }
}
