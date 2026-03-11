using AutoMapper;
using capstone_backend.Business.DTOs.Voucher;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class VoucherProfile : Profile
    {
        public VoucherProfile()
        {
            CreateMap<CreateVoucherRequest, Voucher>();
            CreateMap<Voucher, VoucherResponse>();
        }
    }
}
