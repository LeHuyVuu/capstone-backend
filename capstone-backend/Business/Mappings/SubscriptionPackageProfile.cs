using AutoMapper;
using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class SubscriptionPackageProfile : Profile
    {
        public SubscriptionPackageProfile()
        {
            CreateMap<SubscriptionPackage, SubscriptionPackageDto>();
        }
    }
}
