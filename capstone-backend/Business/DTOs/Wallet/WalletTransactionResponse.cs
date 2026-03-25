using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Wallet
{
    public class WalletTransactionResponse
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public int UserId { get; set; }
        public string? PaymentMethod { get; set; }
        public string TransType { get; set; } = "WALLET_TOPUP";
        public int DocNo { get; set; }
        public string? Description { get; set; }

        // Momo return
        public string? PayUrl { get; set; }
        public string? DeepLink { get; set; }
        public string? QrCodeUrl { get; set; }
        public string? DeeplinkMiniApp { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }

        public bool IsSuccess => Status == TransactionStatus.SUCCESS.ToString();
    }
}
