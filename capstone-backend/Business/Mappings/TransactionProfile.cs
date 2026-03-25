using AutoMapper;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Mappings
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, WalletTransactionHistoryResponse>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => GetTransactionTypeName(src.TransType)));

            CreateMap<Transaction, WalletTransactionResponse>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TransType, opt => opt.MapFrom(src => GetTransactionTypeName(src.TransType)));
        }

        private string GetTransactionTypeName(int transType)
        {
            return transType switch
            {
                1 => "VENUE_SUBSCRIPTION",
                2 => "ADS_ORDER",
                3 => "MEMBER_SUBSCRIPTION",
                4 => "WALLET_TOPUP",
                6 => "MONEY_TO_POINT",
                _ => "UNKNOWN"
            };
        }
    }
}
