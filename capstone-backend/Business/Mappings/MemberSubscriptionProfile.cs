using AutoMapper;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class MemberSubscriptionProfile : Profile
    {
        public MemberSubscriptionProfile()
        {
            CreateMap<Transaction, TransactionResponse>();
        }
    }
}
