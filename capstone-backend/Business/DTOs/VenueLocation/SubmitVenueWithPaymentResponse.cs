namespace capstone_backend.Business.DTOs.VenueLocation;

public class SubmitVenueWithPaymentResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> MissingFields { get; set; } = new();
    
    // Payment info
    public int TransactionId { get; set; }
    public int SubscriptionId { get; set; }
    public string QrCodeUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BankInfo BankInfo { get; set; } = new();
    public DateTime ExpireAt { get; set; }
    public string PaymentContent { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
}

public class BankInfo
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}
