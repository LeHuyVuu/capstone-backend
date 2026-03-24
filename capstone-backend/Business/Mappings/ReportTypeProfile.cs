using AutoMapper;
using capstone_backend.Business.DTOs.Report;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings;

public class ReportTypeProfile : Profile
{
    public ReportTypeProfile()
    {
        CreateMap<ReportType, ReportTypeResponse>();
    }
}
