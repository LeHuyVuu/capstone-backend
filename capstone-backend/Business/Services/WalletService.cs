using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using capstone_backend.Business.DTOs.MoneyToPoint;

namespace capstone_backend.Business.Services;

public class WalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemConfigService _systemConfigService;

    public WalletService(IUnitOfWork unitOfWork, ISystemConfigService systemConfigService)
    {
        _unitOfWork = unitOfWork;
        _systemConfigService = systemConfigService;
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

    public async Task<PagedResult<WalletTransactionHistoryResponse>> GetWalletTransactionHistoryAsync(
        int userId, 
        int pageNumber = 1, 
        int pageSize = 20)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // Max 100 items per page

        // Lấy tất cả giao dịch liên quan đến wallet của user
        // Các trường hợp làm thay đổi số dư wallet:
        // 1. TĂNG SỐ DƯ (+):
        //    - TransType = 4 (WALLET_TOPUP): Nạp tiền vào wallet qua MoMo
        //    - PaymentMethod = "REFUND": Hoàn tiền vào wallet (khi hủy subscription/ads)
        // 2. GIẢM SỐ DƯ (-):
        //    - PaymentMethod = "WALLET": Thanh toán subscription/ads bằng wallet
        // CHỈ LẤY GIAO DỊCH THÀNH CÔNG (SUCCESS) vì chỉ có giao dịch này mới thực sự thay đổi số dư
        var query = _unitOfWork.Context.Set<Transaction>()
            .Where(t => t.UserId == userId && 
                       t.Status == TransactionStatus.SUCCESS.ToString() &&
                       (t.TransType == (int)TransactionType.WALLET_TOPUP || 
                        t.PaymentMethod == "WALLET" ||
                        t.PaymentMethod == "REFUND"));

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated data
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = transactions.Select(t =>
        {
            // Xác định hướng giao dịch
            // Tăng số dư (IN):
            //   - WALLET_TOPUP: Nạp tiền vào wallet
            //   - REFUND: Hoàn tiền vào wallet
            // Giảm số dư (OUT):
            //   - PaymentMethod = WALLET: Thanh toán bằng wallet
            bool isIncoming = t.TransType == (int)TransactionType.WALLET_TOPUP || 
                             t.PaymentMethod == "REFUND";
            string direction = isIncoming ? "IN" : "OUT";
            decimal balanceChange = isIncoming ? t.Amount : -t.Amount;

            return new WalletTransactionHistoryResponse
            {
                TransactionId = t.Id,
                Amount = t.Amount,
                Currency = t.Currency,
                PaymentMethod = t.PaymentMethod,
                TransactionType = GetTransactionTypeName(t.TransType),
                Description = t.Description,
                Status = t.Status ?? "UNKNOWN",
                CreatedAt = t.CreatedAt ?? DateTime.UtcNow,
                Direction = direction,
                BalanceChange = balanceChange
            };
        }).ToList();

        return new PagedResult<WalletTransactionHistoryResponse>(items, pageNumber, pageSize, totalCount);
    }

    private string GetTransactionTypeName(int transType)
    {
        return transType switch
        {
            1 => "VENUE_SUBSCRIPTION",
            2 => "ADS_ORDER",
            3 => "MEMBER_SUBSCRIPTION",
            4 => "WALLET_TOPUP",
            _ => "UNKNOWN"
        };
    }

    public async Task<ConvertMoneyToPointResponse> ConvertMoneyToPointAsync(int userId, ConvertMoneyToPointRequest request)
    {
        if (request == null)
            throw new Exception("Dữ liệu không hợp lệ");

        if (request.Amount <= 0)
            throw new Exception("Số tiền đổi phải lớn hơn 0");

        var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (member == null)
            throw new Exception("Thành viên không tồn tại");

        var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
        if (wallet == null)
            throw new Exception("Ví không tồn tại");

        if (wallet.IsActive == false)
            throw new Exception("Ví không hoạt động");

        var rate = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.MONEY_TO_POINT_RATE.ToString());
        if (rate <= 0)
            throw new Exception("Cấu hình tỉ lệ đổi point không hợp lệ");

        if (request.Amount % rate != 0)
            throw new Exception($"Số tiền đổi phải là bội số của {rate}");

        if (wallet.Balance < request.Amount)
            throw new Exception("Số dư ví không đủ");

        var pointsToAdd = (int)(request.Amount / rate);
        if (pointsToAdd <= 0)
            throw new Exception("Số point quy đổi không hợp lệ");

        var balanceBefore = wallet.Balance;
        var pointsBefore = wallet.Points ?? 0;

        wallet.Balance = Math.Max(0, wallet.Balance.Value - request.Amount);
        wallet.Points = pointsBefore + pointsToAdd;
        wallet.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Wallets.Update(wallet);

        var transaction = new Transaction
        {
            Amount = request.Amount,
            Currency = "VND",
            UserId = userId,
            Description = $"Convert {request.Amount} VND to {pointsToAdd} points",
            PaymentMethod = PaymentMethod.SYSTEM.ToString(),
            TransType = 6,
            DocNo = wallet.Id,
            ExternalRefCode = null,
            Status = TransactionStatus.SUCCESS.ToString()
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return new ConvertMoneyToPointResponse
        {
            ConvertedMoney = request.Amount,
            ConvertedPoints = pointsToAdd,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            PointsBefore = pointsBefore,
            PointsAfter = wallet.Points ?? 0,
            Rate = rate
        };
    }
}
