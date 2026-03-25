namespace capstone_backend.Business.DTOs.Wallet;

public class ExternalRefCodeDto
{
    public long SepayTransactionId { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? QrData { get; set; }
    public string? OrderCode { get; set; }
    public DateTime? ExpireAt { get; set; }
    public BankInfoDto? BankInfo { get; set; }
}
