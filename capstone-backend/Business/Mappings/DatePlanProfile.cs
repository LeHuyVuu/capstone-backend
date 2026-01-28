using AutoMapper;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class DatePlanProfile : Profile
    {
        public DatePlanProfile()
        {
            CreateMap<CreateDatePlanRequest, DatePlan>();
        }
    }
}
