using AutoMapper;
using capstone_backend.Business.DTOs.DatePlan;
using capstone_backend.Business.DTOs.DatePlanItem;
using capstone_backend.Data.Entities;
using capstone_backend.Extensions.Common;

namespace capstone_backend.Business.Mappings
{
    public class DatePlanProfile : Profile
    {
        public DatePlanProfile()
        {
            CreateMap<CreateDatePlanRequest, DatePlan>();
            CreateMap<DatePlan, DatePlanResponse>()
                .ForMember(dest => dest.PlannedStartAt, opt => opt.MapFrom(src =>
                    src.PlannedStartAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.PlannedStartAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.PlannedEndAt, opt => opt.MapFrom(src =>
                    src.PlannedEndAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.PlannedEndAt.Value)
                        : (DateTime?)null));
            CreateMap<DatePlan, DatePlanDetailResponse>()
                .ForMember(dest => dest.PlannedStartAt, opt => opt.MapFrom(src =>
                    src.PlannedStartAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.PlannedStartAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.PlannedEndAt, opt => opt.MapFrom(src =>
                    src.PlannedEndAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.PlannedEndAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => 
                    src.CompletedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.CompletedAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src =>
                    src.UpdatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.UpdatedAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
                    src.CreatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.CreatedAt.Value)
                        : (DateTime?)null))
                .ForMember(dest => dest.Venues, opt => opt.MapFrom(src => src.DatePlanItems));

            CreateMap<DatePlanItemRequest, DatePlanItem>();
            CreateMap<DatePlanItem, DatePlanItemResponse>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src =>
                    src.CreatedAt.HasValue
                        ? TimezoneUtil.ToVietNamTime(src.CreatedAt.Value)
                        : (DateTime?)null));
        }
    }
}
