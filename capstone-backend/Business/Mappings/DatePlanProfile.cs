using AutoMapper;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class DatePlanProfile : Profile
    {
        public DatePlanProfile()
        {
            CreateMap<CreateDatePlanRequest, DatePlan>();
            CreateMap<DatePlan, DatePlanResponse>();
            CreateMap<DatePlan, DatePlanDetailResponse>()
                .ForMember(dest => dest.Venues, opt => opt.MapFrom(src => src.DatePlanItems));

            CreateMap<DatePlanItemRequest, DatePlanItem>();
            CreateMap<DatePlanItem, DatePlanItemResponse>();
        }
    }
}
