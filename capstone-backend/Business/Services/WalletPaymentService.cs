using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service xử lý thanh toán qua Wallet
/// Tách riêng để không ảnh hưởng logic VietQR cũ
/// </summary>
public class WalletPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletPaymentService> _logger;

    public WalletPaymentService(
        IUnitOfWork unitOfWork,
        ILogger<WalletPaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Xử lý thanh toán qua wallet - trừ tiền và cập nhật transaction
    /// KHÔNG tự tạo transaction, sử dụng transaction từ caller
    /// </summary>
    public async Task<WalletPaymentResult> ProcessWalletPaymentAsync(
        int userId,
        decimal amount,
        int transactionId,
        string description)
    {
        _logger.LogInformation("Processing wallet payment - UserId: {UserId}, Amount: {Amount}, TxId: {TxId}",
            userId, amount, transactionId);

        // 1. Get wallet (không cần lock vì caller đã có transaction)
        var wallet = await _unitOfWork.Context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.IsActive == true);

        if (wallet == null)
        {
            return new WalletPaymentResult
            {
                IsSuccess = false,
                Message = "Wallet not found or inactive. Please contact support."
            };
        }

        // 2. Check balance
        var currentBalance = wallet.Balance ?? 0;
        if (currentBalance < amount)
        {
            return new WalletPaymentResult
            {
                IsSuccess = false,
                Message = $"Insufficient balance. Available: {currentBalance:N0} VND, Required: {amount:N0} VND"
            };
        }

        // 3. Get transaction
        var transaction = await _unitOfWork.Context.Set<Transaction>()
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            return new WalletPaymentResult
            {
                IsSuccess = false,
                Message = "Transaction not found"
            };
        }

        // 4. Validate transaction state
        if (transaction.Status != TransactionStatus.PENDING.ToString())
        {
            return new WalletPaymentResult
            {
                IsSuccess = false,
                Message = $"Transaction status is {transaction.Status}, cannot process payment"
            };
        }

        // 5. Deduct balance (trong transaction của caller)
        var oldBalance = wallet.Balance ?? 0;
        wallet.Balance = oldBalance - amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Context.Set<Wallet>().Update(wallet);

        // 6. Update transaction to SUCCESS
        transaction.Status = TransactionStatus.SUCCESS.ToString();
        transaction.UpdatedAt = DateTime.UtcNow;
        
        // Store wallet payment metadata
        var metadata = new Dictionary<string, object>
        {
            { "paymentMethod", "WALLET" },
            { "walletId", wallet.Id },
            { "oldBalance", oldBalance },
            { "newBalance", wallet.Balance ?? 0 },
            { "paidAt", DateTime.UtcNow.ToString("O") }
        };
        transaction.ExternalRefCode = System.Text.Json.JsonSerializer.Serialize(metadata);

        _unitOfWork.Context.Set<Transaction>().Update(transaction);

        // 7. Không SaveChanges ở đây, để caller tự save
        _logger.LogInformation("✅ Wallet payment prepared - TxId: {TxId}, WalletId: {WalletId}, Balance: {OldBalance} → {NewBalance}",
            transactionId, wallet.Id, oldBalance, wallet.Balance);

        return new WalletPaymentResult
        {
            IsSuccess = true,
            Message = "Payment successful",
            TransactionId = transaction.Id,
            OldBalance = oldBalance,
            NewBalance = wallet.Balance ?? 0,
            AmountPaid = amount
        };
    }

    /// <summary>
    /// Check if user has sufficient wallet balance
    /// </summary>
    public async Task<(bool HasSufficient, decimal CurrentBalance)> CheckWalletBalanceAsync(int userId, decimal requiredAmount)
    {
        var wallet = await _unitOfWork.Context.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.IsActive == true);

        if (wallet == null)
        {
            return (false, 0);
        }

        var balance = wallet.Balance ?? 0;
        return (balance >= requiredAmount, balance);
    }
}

/// <summary>
/// Result của wallet payment
/// </summary>
public class WalletPaymentResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TransactionId { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public decimal AmountPaid { get; set; }
}
