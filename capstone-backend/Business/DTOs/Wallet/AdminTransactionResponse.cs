namespace capstone_backend.Business.DTOs.Wallet;

public class AdminTransactionResponse
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string TransactionType { get; set; } = null!;
    public int DocNo { get; set; }
    public string? Description { get; set; }
    public ExternalRefCodeDto? ExternalRefCode { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
