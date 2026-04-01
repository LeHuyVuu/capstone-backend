using Amazon.Rekognition.Model;
using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.MoneyToPoint;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace capstone_backend.Business.Services;

public class WalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemConfigService _systemConfigService;
    private readonly IMapper _mapper;

    public WalletService(IUnitOfWork unitOfWork, ISystemConfigService systemConfigService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _systemConfigService = systemConfigService;
        _mapper = mapper;
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
        if (pageSize > 100) pageSize = 100; 

        var query = _unitOfWork.Context.Set<Transaction>()
            .Where(t => t.UserId == userId && 
                       t.Status == TransactionStatus.SUCCESS.ToString() &&
                       (t.TransType == (int)TransactionType.WALLET_TOPUP || 
                        t.TransType == (int)TransactionType.VENUE_SETTLEMENT_PAYOUT ||
                        t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION ||
                        t.TransType == (int)TransactionType.ADS_ORDER ||
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
            // Xác định hướng giao dịch cho VENUE OWNER
            // Tăng số dư (IN):
            //   - WALLET_TOPUP: Nạp tiền vào wallet
            //   - VENUE_SETTLEMENT_PAYOUT: Thanh toán từ admin cho venue owner
            //   - REFUND: Hoàn tiền vào wallet
            // Giảm số dư (OUT):
            //   - VENUE_SUBSCRIPTION: Thanh toán subscription cho venue
            //   - ADS_ORDER: Thanh toán quảng cáo
            //   - PaymentMethod = WALLET: Thanh toán bằng wallet
            bool isIncoming = t.TransType == (int)TransactionType.WALLET_TOPUP || 
                             t.TransType == (int)TransactionType.VENUE_SETTLEMENT_PAYOUT ||
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
            5 => "VENUE_SETTLEMENT_PAYOUT",
            6 => "MONEY_TO_POINT",
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

    public async Task<PagedResult<AdminTransactionResponse>> GetAllTransactionsForAdminAsync(
        int pageNumber = 1, 
        int pageSize = 20,
        string? status = null,
        int? transType = null,
        int? userId = null)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _unitOfWork.Context.Set<Transaction>().AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (transType.HasValue)
        {
            query = query.Where(t => t.TransType == transType.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Get paginated data with user info
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                Transaction = t,
                User = _unitOfWork.Context.Set<UserAccount>().FirstOrDefault(u => u.Id == t.UserId)
            })
            .ToListAsync();

        var items = transactions.Select(t =>
        {
            ExternalRefCodeDto? externalRefCode = null;
            if (!string.IsNullOrEmpty(t.Transaction.ExternalRefCode))
            {
                try
                {
                    externalRefCode = System.Text.Json.JsonSerializer.Deserialize<ExternalRefCodeDto>(t.Transaction.ExternalRefCode);
                }
                catch
                {
                    // If parsing fails, leave it as null
                }
            }

            return new AdminTransactionResponse
            {
                TransactionId = t.Transaction.Id,
                UserId = t.Transaction.UserId,
                UserName = t.User != null ? t.User.DisplayName : null,
                UserEmail = t.User != null ? t.User.Email : null,
                Amount = t.Transaction.Amount,
                Currency = t.Transaction.Currency,
                PaymentMethod = t.Transaction.PaymentMethod,
                TransactionType = GetTransactionTypeName(t.Transaction.TransType),
                DocNo = t.Transaction.DocNo,
                Description = t.Transaction.Description,
                ExternalRefCode = externalRefCode,
                Status = t.Transaction.Status ?? "UNKNOWN",
                CreatedAt = t.Transaction.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = t.Transaction.UpdatedAt
            };
        }).ToList();

        return new PagedResult<AdminTransactionResponse>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResult<WalletTransactionHistoryResponse>> GetMemberWalletTransactionHistoryAsync(int userId, int pageNumber, int pageSize)
    {
        var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (member == null)
            throw new Exception("Thành viên không tồn tại");

        var wallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
        if (wallet == null)
            throw new Exception("Ví không tồn tại");

        var (transactions, totalCount) = await _unitOfWork.Transactions.GetPagedAsync(
            pageNumber,
            pageSize,
            t => t.UserId == userId &&
                 t.Status == TransactionStatus.SUCCESS.ToString() &&
                 (t.TransType == 4 || t.TransType == 6 || t.TransType == 3),
            t => t.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
        );

        var response = _mapper.Map<List<WalletTransactionHistoryResponse>>(transactions);

        foreach (var item in response)
        {
            if (item.TransactionType == TransactionType.WALLET_TOPUP.ToString())
            {
                item.Direction = "IN";
                item.BalanceChange = item.Amount;
            }
            else if (item.TransactionType == TransactionType.MONEY_TO_POINT.ToString())
            {
                item.Direction = "OUT";
                item.BalanceChange = -item.Amount;
            }
            else
            {
                item.Direction = "IN";
                item.BalanceChange = item.Amount;
            }
        }

        return new PagedResult<WalletTransactionHistoryResponse>
        {
            Items = response,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<WalletExchangeRateResponse> GetMoneyToPointExchangeRateAsync(int userId)
    {
        var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (member == null)
            throw new Exception("Thành viên không tồn tại");

        var rate = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.MONEY_TO_POINT_RATE.ToString());
        if (rate <= 0)
            throw new Exception("Cấu hình tỉ lệ đổi point không hợp lệ");

        return new WalletExchangeRateResponse
        {
            MoneyAmount = rate,
            PointAmount = 1,
            Description = $"Tỉ lệ đổi: {rate} VND = 1 point"
        };
    }

    public async Task<TransactionResponse> CheckMomoPaymentStatusAsync(int userId, string orderId)
    {
        var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (member == null)
            throw new Exception("Hồ sơ thành viên không tồn tại");

        var orderParts = orderId.Split("_");
        if (orderParts.Length < 3)
            throw new Exception("Order ID không hợp lệ");
        var transactionId = IdEncoder.Decode(orderParts[2]);

        var tx = await _unitOfWork.Transactions.GetByIdAsync((int)transactionId);
        if (tx == null || tx.UserId != userId)
            throw new Exception("Giao dịch không tồn tại hoặc không thuộc về người dùng");

        if (tx.TransType != 3 && tx.TransType != 4)
            throw new Exception("Giao dịch không phải là nạp tiền vào ví hoặc thanh toán gói member");

        var response = _mapper.Map<TransactionResponse>(tx);

        if (tx.TransType == 3) {
            var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
            if (sub == null)
                throw new Exception("Không ghi nhận được gói đăng ký của member");

            response.MemberSubscriptionId = tx.DocNo;
            response.StartDate = sub.StartDate;
            response.EndDate = sub.EndDate;
            response.IsActive = sub.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString();
        }

        var metadata = JsonConverterUtil.DeserializeOrDefault<MomoTransactionMetadata>(tx.ExternalRefCode);
        response.PayUrl = metadata?.PayUrl;
        response.QrCodeUrl = metadata?.QrCodeUrl;
        response.DeepLink = metadata?.DeepLink;
        response.DeeplinkMiniApp = metadata?.DeeplinkMiniApp;

        return response;
    }
}
