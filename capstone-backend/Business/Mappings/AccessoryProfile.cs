using AutoMapper;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class AccessoryProfile : Profile
    {
        public AccessoryProfile()
        {
            CreateMap<Accessory, AccessoryResponse>();
        }
    }
}
