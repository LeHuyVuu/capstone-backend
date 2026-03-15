using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class WalletService
{
    private readonly IUnitOfWork _unitOfWork;

    public WalletService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WalletBalanceResponse?> GetWalletBalanceAsync(int userId)
    {
        var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);

        if (wallet == null)
            return null;

        return new WalletBalanceResponse
        {
            WalletId = wallet.Id,
            Balance = wallet.Balance ?? 0,
            Points = wallet.Points ?? 0,
            IsActive = wallet.IsActive ?? false
        };
    }

    public async Task<WithdrawRequestResponse> CreateWithdrawRequestAsync(int userId, CreateWithdrawRequestRequest request)
    {
        var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);

        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        if (wallet.IsActive != true)
            throw new InvalidOperationException("Wallet is not active");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than 0");

        if (request.Amount > wallet.Balance)
            throw new InvalidOperationException($"Insufficient balance. Available: {wallet.Balance:N0} VND");

        var withdrawRequest = new WithdrawRequest
        {
            WalletId = wallet.Id,
            Amount = request.Amount,
            BankInfo = System.Text.Json.JsonSerializer.Serialize(request.BankInfo),
            Status = WithdrawRequestStatus.PENDING.ToString(),
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.WithdrawRequests.AddAsync(withdrawRequest);
        await _unitOfWork.SaveChangesAsync();

        return new WithdrawRequestResponse
        {
            Id = withdrawRequest.Id,
            WalletId = wallet.Id,
            Amount = withdrawRequest.Amount ?? 0,
            BankInfo = request.BankInfo,
            Status = withdrawRequest.Status ?? WithdrawRequestStatus.PENDING.ToString(),
            RequestedAt = withdrawRequest.RequestedAt ?? DateTime.UtcNow
        };
    }

    public async Task<List<WithdrawRequestResponse>> GetMyWithdrawRequestsAsync(int userId)
    {
        var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);

        if (wallet == null)
            return new List<WithdrawRequestResponse>();

        var requests = await _unitOfWork.WithdrawRequests.GetByWalletIdAsync(wallet.Id);

        return requests.Select(wr =>
        {
            BankInfoDto? bankInfo = null;
            try
            {
                if (!string.IsNullOrEmpty(wr.BankInfo))
                {
                    bankInfo = System.Text.Json.JsonSerializer.Deserialize<BankInfoDto>(wr.BankInfo);
                }
            }
            catch { }

            return new WithdrawRequestResponse
            {
                Id = wr.Id,
                WalletId = wr.WalletId,
                Amount = wr.Amount ?? 0,
                BankInfo = bankInfo ?? new BankInfoDto { BankName = "", AccountNumber = "", AccountName = "" },
                Status = wr.Status ?? WithdrawRequestStatus.PENDING.ToString(),
                RejectionReason = wr.RejectionReason,
                ProofImageUrl = wr.ProofImageUrl,
                RequestedAt = wr.RequestedAt ?? DateTime.UtcNow
            };
        }).ToList();
    }
}
