namespace capstone_backend.Business.DTOs.Wallet;

public class VenueOwnerWalletTopupResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;

    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";

    public string PaymentContent { get; set; } = string.Empty;
    public string? QrCodeUrl { get; set; }
    public DateTime? ExpireAt { get; set; }
    public BankInfoDto? BankInfo { get; set; }
}
