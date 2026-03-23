namespace capstone_backend.Business.DTOs.Wallet;

public class WalletTransactionHistoryResponse
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string TransactionType { get; set; } = null!;
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Hướng giao dịch: "IN" (tăng số dư) hoặc "OUT" (giảm số dư)
    /// </summary>
    public string Direction { get; set; } = null!;
    
    /// <summary>
    /// Số tiền thay đổi: dương (+) nếu tăng, âm (-) nếu giảm
    /// </summary>
    public decimal BalanceChange { get; set; }
}
