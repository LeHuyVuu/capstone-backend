using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentController> _logger;
    private readonly IMomoService _momoService;
    private readonly WalletService _walletService;

    public PaymentController(
        IUnitOfWork unitOfWork,
        ILogger<PaymentController> logger,
        IMomoService momoService,
        WalletService walletService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _momoService = momoService;
        _walletService = walletService;
    }

    /// <summary>
    /// Check payment status - FE dùng để polling hoặc check một lần
    /// </summary>
    /// <param name="transactionId">Transaction ID từ response submit-with-payment</param>
    /// <returns>Payment status and subscription info nếu thành công</returns>
    [HttpGet("status/{transactionId}")]
    public async Task<IActionResult> CheckPaymentStatus(int transactionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResponse("Unauthorized");
        }

        var transaction = await _unitOfWork.Context.Set<Transaction>()
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId.Value);

        if (transaction == null)
        {
            return NotFoundResponse("Transaction not found");
        }

        // Get subscription info if payment is for venue subscription
        VenueSubscriptionPackage? subscription = null;
        if (transaction.TransType == 1) // VENUE_SUBSCRIPTION
        {
            subscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Include(s => s.Package)
                .Include(s => s.Venue)
                .FirstOrDefaultAsync(s => s.Id == transaction.DocNo);
        }

        var response = new
        {
            transactionId = transaction.Id,
            status = transaction.Status, // PENDING, SUCCESS, FAILED, EXPIRED, CANCELLED
            amount = transaction.Amount,
            currency = transaction.Currency,
            paymentMethod = transaction.PaymentMethod,
            description = transaction.Description,
            createdAt = transaction.CreatedAt,
            updatedAt = transaction.UpdatedAt,
            subscription = subscription == null ? null : new
            {
                id = subscription.Id,
                status = subscription.Status, // PENDING_PAYMENT, ACTIVE, EXPIRED, CANCELLED
                startDate = subscription.StartDate,
                endDate = subscription.EndDate,
                packageName = subscription.Package?.PackageName,
                venue = new
                {
                    id = subscription.Venue?.Id,
                    name = subscription.Venue?.Name,
                    status = subscription.Venue?.Status // PENDING, APPROVED, REJECTED
                }
            },
            // Parse external ref for QR info
            externalInfo = string.IsNullOrEmpty(transaction.ExternalRefCode)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.ExternalRefCode)
        };

        return Ok(ApiResponse<object>.Success(response));
    }

    /// <summary>
    /// Get QR code info để hiển thị lại (nếu user đóng popup)
    /// </summary>
    [HttpGet("qr-info/{transactionId}")]
    public async Task<IActionResult> GetQrInfo(int transactionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResponse("Unauthorized");
        }

        var transaction = await _unitOfWork.Context.Set<Transaction>()
            .FirstOrDefaultAsync(t => t.Id == transactionId
                && t.UserId == userId.Value
                && t.Status == TransactionStatus.PENDING.ToString());

        if (transaction == null)
        {
            return NotFoundResponse("Transaction not found or already completed");
        }

        if (string.IsNullOrEmpty(transaction.ExternalRefCode))
        {
            return BadRequestResponse("QR code info not available");
        }

        var externalRef = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(transaction.ExternalRefCode);

        var response = new
        {
            transactionId = transaction.Id,
            qrCodeUrl = externalRef?.GetValueOrDefault("qrCodeUrl").GetString(),
            amount = transaction.Amount,
            paymentContent = transaction.Description,
            expireAt = externalRef?.GetValueOrDefault("expireAt").GetDateTime(),
            bankInfo = externalRef?.GetValueOrDefault("bankInfo")
        };

        return OkResponse(response);
    }

    /// <summary>
    /// Cancel pending payment (nếu user không muốn thanh toán nữa)
    /// </summary>
    [HttpPost("cancel/{transactionId}")]
    public async Task<IActionResult> CancelPayment(int transactionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResponse("Unauthorized");
        }

        var transaction = await _unitOfWork.Context.Set<Transaction>()
            .FirstOrDefaultAsync(t => t.Id == transactionId
                && t.UserId == userId.Value
                && t.Status == TransactionStatus.PENDING.ToString());

        if (transaction == null)
        {
            return NotFoundResponse("Transaction not found or already completed");
        }

        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

        try
        {
            transaction.Status = TransactionStatus.CANCELLED.ToString();
            transaction.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Context.Set<Transaction>().Update(transaction);

            // Handle VENUE_SUBSCRIPTION (TransType = 1)
            if (transaction.TransType == 1)
            {
                var subscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                    .FirstOrDefaultAsync(s => s.Id == transaction.DocNo
                        && s.Status == VenueSubscriptionPackageStatus.PENDING_PAYMENT.ToString());

                if (subscription != null)
                {
                    subscription.Status = VenueSubscriptionPackageStatus.CANCELLED.ToString();
                    subscription.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                }
            }
            // Handle ADS_ORDER (TransType = 2)
            else if (transaction.TransType == 2)
            {
                var adsOrder = await _unitOfWork.Context.Set<AdsOrder>()
                    .FirstOrDefaultAsync(ao => ao.Id == transaction.DocNo
                        && ao.Status == AdsOrderStatus.PENDING.ToString());

                if (adsOrder != null)
                {
                    adsOrder.Status = AdsOrderStatus.CANCELLED.ToString();
                    adsOrder.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Context.Set<AdsOrder>().Update(adsOrder);

                    // Cancel related VenueLocationAdvertisements
                    var venueAds = await _unitOfWork.Context.Set<VenueLocationAdvertisement>()
                        .Where(vla => vla.AdvertisementId == adsOrder.AdvertisementId
                            && vla.Status == VenueLocationAdvertisementStatus.PENDING_PAYMENT.ToString())
                        .ToListAsync();

                    foreach (var vla in venueAds)
                    {
                        vla.Status = VenueLocationAdvertisementStatus.CANCELLED.ToString();
                        vla.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Context.Set<VenueLocationAdvertisement>().Update(vla);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return OkResponse(new { message = "Payment cancelled successfully" });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling payment");
            return InternalServerErrorResponse("Failed to cancel payment");
        }
    }

    /// <summary>
    /// Process subscription payment (For Members)
    /// </summary>
    [Authorize(Roles = "MEMBER, member")]
    [HttpPost("member/momo-pay")]
    public async Task<IActionResult> ProcessMemberSubscriptionPayment([FromBody] ProcessMemberSubscriptionPaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("Unauthorized");
            }

            var result = await _momoService.ProcessMemberSubscriptionPaymentAsync(userId.Value, request);
            if (result == null)
                return BadRequestResponse("Lấy link thanh toán thất bại");

            return OkResponse(result, "Lấy link thanh toán thành công");
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Process wallet top-up payment (For Members)
    /// </summary>
    [HttpPost("member/momo-topup")]
    public async Task<IActionResult> ProcessMemberWalletTopup([FromBody] CreateWalletTopupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("Unauthorized");
            }
            var result = await _momoService.ProcessMemberWalletTopupAsync(userId.Value, request);
            if (result == null)
                return BadRequestResponse("Lấy link thanh toán thất bại");
            return OkResponse(result, "Lấy link thanh toán thành công");
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Check payment status for wallet top-up (For Members)
    /// </summary>
    [HttpGet("top-up/status/{orderId}")]
    public async Task<IActionResult> CheckWalletTopupStatus([FromRoute] string orderId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("Unauthorized");
            }
            var result = await _walletService.CheckWalletTopupStatusAsync(userId.Value, orderId);
            if (result == null)
            {
                return NotFoundResponse("Giao dịch không khả dụng");
            }
            return OkResponse(result, "Lấy trạng thái giao dịch thành công");
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}
