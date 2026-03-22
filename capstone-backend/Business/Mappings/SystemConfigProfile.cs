using AutoMapper;
using capstone_backend.Business.DTOs.SystemConfig;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class SystemConfigProfile : Profile
    {
        public SystemConfigProfile()
        {
            CreateMap<SystemConfig, SystemConfigResponse>();
        }
    }
}
