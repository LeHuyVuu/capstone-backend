namespace capstone_backend.Business.DTOs.Wallet;

public class WithdrawRequestResponse
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public BankInfoDto BankInfo { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? ProofImageUrl { get; set; }
    public DateTime RequestedAt { get; set; }
}
