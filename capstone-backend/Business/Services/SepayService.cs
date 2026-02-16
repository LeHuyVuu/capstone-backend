using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace capstone_backend.Business.Services;

/// <summary>
/// Sepay payment service - generates VietQR codes and receives automatic webhooks
/// Sepay monitors your bank account and sends webhooks when payments are received
/// </summary>
public class SepayService
{
    private readonly ILogger<SepayService> _logger;
    private readonly string _accountNumber;
    private readonly string _accountName;
    private readonly string _bankName;
    private readonly string _bankBin;

    public SepayService(ILogger<SepayService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _accountNumber = configuration["Sepay:AccountNumber"] ?? throw new InvalidOperationException("Sepay:AccountNumber not configured");
        _accountName = configuration["Sepay:AccountName"] ?? "NGUYEN VAN A";
        _bankName = configuration["Sepay:BankName"] ?? "TPBank";
        _bankBin = configuration["Sepay:BankBin"] ?? "970423"; // TPBank BIN
    }

    /// <summary>
    /// Generate VietQR payment code - Sepay will monitor bank account and send webhook when paid
    /// </summary>
    public Task<SepayTransactionResponse> CreateTransactionAsync(decimal amount, string description, string orderId)
    {
        try
        {
            var amountInt = (int)amount;
            
            // Generate VietQR URL (standard format supported by all Vietnamese banks)
            var encodedContent = HttpUtility.UrlEncode(description);
            var encodedAccountName = HttpUtility.UrlEncode(_accountName);
            
            // VietQR.io format: compact2 template for cleaner QR
            var qrUrl = $"https://img.vietqr.io/image/{_bankBin}-{_accountNumber}-compact2.jpg?amount={amountInt}&addInfo={encodedContent}&accountName={encodedAccountName}";
            
            _logger.LogInformation("✅ Generated VietQR code - Amount: {Amount}đ, OrderCode: {OrderCode}", amount, orderId);

            // Return response compatible with existing code
            var response = new SepayTransactionResponse
            {
                Status = 200,
                Messages = new SepayMessages { Success = "QR code generated" },
                Data = new SepayTransactionData
                {
                    Id = 0, // Not applicable for local QR generation
                    Amount = amountInt,
                    Content = description,
                    OrderCode = orderId,
                    BankAccount = _accountNumber,
                    QrCode = qrUrl, // QR code URL (not Base64, FE can display as image)
                    QrData = description
                }
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating VietQR code");
            throw;
        }
    }

    public (string BankName, string AccountNumber, string AccountName) GetBankInfo()
    {
        return (_bankName, _accountNumber, _accountName);
    }
}

#region Sepay DTOs

public class SepayTransactionResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("messages")]
    public SepayMessages? Messages { get; set; }

    [JsonPropertyName("data")]
    public SepayTransactionData? Data { get; set; }
}

public class SepayMessages
{
    [JsonPropertyName("success")]
    public string? Success { get; set; }
}

public class SepayTransactionData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("order_code")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("bank_account")]
    public string? BankAccount { get; set; }

    [JsonPropertyName("qr_code")]
    public string? QrCode { get; set; } // Base64 image

    [JsonPropertyName("qr_data")]
    public string? QrData { get; set; } // QR data string
}

/// <summary>
/// Webhook data từ Sepay khi có thanh toán
/// </summary>
public class SepayWebhookData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("transaction_date")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("order_code")]
    public string? OrderCode { get; set; }

    [JsonPropertyName("bank_account")]
    public string? BankAccount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } // "success" | "pending" | "failed"
}

#endregion
