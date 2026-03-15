using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class RefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IUnitOfWork unitOfWork,
        ILogger<RefundService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Process refund to user's wallet
    /// NOTE: This method does NOT create its own transaction. 
    /// Caller must manage the transaction if needed.
    /// </summary>
    /// <param name="userId">User ID to refund to</param>
    /// <param name="amount">Amount to refund</param>
    /// <param name="transType">Transaction type (1=VENUE_SUBSCRIPTION, 2=ADS_ORDER, etc.)</param>
    /// <param name="docNo">Document number (SubscriptionId, AdsOrderId, etc.)</param>
    /// <param name="reason">Refund reason</param>
    /// <param name="originalTransactionId">Original payment transaction ID (optional)</param>
    /// <param name="metadata">Additional metadata (optional)</param>
    /// <returns>RefundResult with success status and transaction ID</returns>
    public async Task<RefundResult> ProcessRefundAsync(
        int userId,
        decimal amount,
        int transType,
        int docNo,
        string reason,
        int? originalTransactionId = null,
        Dictionary<string, object>? metadata = null)
    {
        if (amount <= 0)
        {
            return new RefundResult
            {
                IsSuccess = false,
                Message = "Refund amount must be greater than 0"
            };
        }

        try
        {
            // 1. Find or create wallet for user
            var wallet = await _unitOfWork.Context.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                // Create new wallet if not exists
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    Points = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Context.Set<Wallet>().AddAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Created new wallet for user {UserId}", userId);
            }

            if (wallet.IsActive != true)
            {
                return new RefundResult
                {
                    IsSuccess = false,
                    Message = "User wallet is not active"
                };
            }

            // 2. Update wallet balance
            var oldBalance = wallet.Balance ?? 0;
            wallet.Balance = oldBalance + amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Context.Set<Wallet>().Update(wallet);

            // 3. Create refund transaction record
            var refundMetadata = metadata ?? new Dictionary<string, object>();
            refundMetadata["refundType"] = GetRefundTypeName(transType);
            refundMetadata["refundedAt"] = DateTime.UtcNow.ToString("O");
            refundMetadata["oldBalance"] = oldBalance;
            refundMetadata["newBalance"] = wallet.Balance;
            
            if (originalTransactionId.HasValue)
            {
                refundMetadata["originalTransactionId"] = originalTransactionId.Value;
            }

            var refundTransaction = new Transaction
            {
                UserId = userId,
                Amount = amount,
                Currency = "VND",
                PaymentMethod = "REFUND",
                TransType = transType,
                DocNo = docNo,
                Description = $"Hoàn tiền: {reason}",
                Status = TransactionStatus.SUCCESS.ToString(),
                ExternalRefCode = System.Text.Json.JsonSerializer.Serialize(refundMetadata),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<Transaction>().AddAsync(refundTransaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Refund processed successfully - UserId: {UserId}, Amount: {Amount} VND, TransType: {TransType}, DocNo: {DocNo}, TxId: {TxId}, Balance: {OldBalance} → {NewBalance}",
                userId, amount, transType, docNo, refundTransaction.Id, oldBalance, wallet.Balance);

            return new RefundResult
            {
                IsSuccess = true,
                Message = "Refund processed successfully",
                TransactionId = refundTransaction.Id,
                WalletId = wallet.Id,
                OldBalance = oldBalance,
                NewBalance = wallet.Balance ?? 0,
                RefundAmount = amount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing refund - UserId: {UserId}, Amount: {Amount}, TransType: {TransType}, DocNo: {DocNo}",
                userId, amount, transType, docNo);

            return new RefundResult
            {
                IsSuccess = false,
                Message = "Failed to process refund"
            };
        }
    }

    /// <summary>
    /// Get refund type name for logging and metadata
    /// </summary>
    private string GetRefundTypeName(int transType)
    {
        return transType switch
        {
            1 => "VENUE_SUBSCRIPTION_REFUND",
            2 => "ADVERTISEMENT_REFUND",
            3 => "MEMBER_SUBSCRIPTION_REFUND",
            _ => "GENERAL_REFUND"
        };
    }
}

/// <summary>
/// Result of refund operation
/// </summary>
public class RefundResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TransactionId { get; set; }
    public int? WalletId { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal RefundAmount { get; set; }
}
