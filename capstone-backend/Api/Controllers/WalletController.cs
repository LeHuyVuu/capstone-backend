using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.MoneyToPoint;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WalletController : BaseController
{
    private readonly WalletService _walletService;

    public WalletController(WalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>VENUE OWNER</summary>
    [HttpGet("balance")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");

        var balance = await _walletService.GetWalletBalanceAsync(userId.Value);

        if (balance == null)
            return NotFoundResponse("Wallet not found");

        return OkResponse(balance, "Wallet balance retrieved successfully");
    }

    /// <summary>VENUE OWNER - Tạo giao dịch nạp tiền vào wallet qua VietQR (chỉ xử lý transaction)</summary>
    [HttpPost("topup")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> CreateTopupTransaction([FromBody] CreateWalletTopupRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");

        try
        {
            var result = await _walletService.CreateVenueOwnerWalletTopupAsync(userId.Value, request);
            return OkResponse(result, "Top-up transaction created successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>VENUE OWNER</summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> CreateWithdrawRequest([FromBody] CreateWithdrawRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");

        try
        {
            var withdrawRequest = await _walletService.CreateWithdrawRequestAsync(userId.Value, request);
            return CreatedResponse(withdrawRequest, "Withdraw request created successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>VENUE OWNER</summary>
    [HttpGet("withdraw-requests")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> GetMyWithdrawRequests()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");

        var requests = await _walletService.GetMyWithdrawRequestsAsync(userId.Value);
        return OkResponse(requests, $"Retrieved {requests.Count} withdraw request(s)");
    }

    /// <summary>VENUE OWNER - Lấy lịch sử giao dịch biến động số dư wallet</summary>
    [HttpGet("transaction-history")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> GetTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");

        var history = await _walletService.GetWalletTransactionHistoryAsync(userId.Value, pageNumber, pageSize);
        return OkResponse(history, $"Retrieved {history.Items.Count()} transaction(s) from page {pageNumber}");
    }

    /// <summary>
    /// MEMBER - Chuyển đổi tiền thành điểm
    /// </summary>
    [Authorize(Roles = "MEMBER, member")]
    [HttpPost("convert-money-to-point")]
    public async Task<IActionResult> ConvertMoneyToPoint([FromBody] ConvertMoneyToPointRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");
        try
        {
            var result = await _walletService.ConvertMoneyToPointAsync(userId.Value, request);
            return OkResponse(result, "Money converted to points successfully");
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// MEMBER - Lấy lịch sử giao dịch biến động số dư cho member
    /// </summary>
    [Authorize(Roles = "MEMBER, member")]
    [HttpGet("member/transactions")]
    public async Task<IActionResult> GetMemberTransactionHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");
        var history = await _walletService.GetMemberWalletTransactionHistoryAsync(userId.Value, pageNumber, pageSize);
        return OkResponse(history, $"Retrieved {history.Items.Count()} transaction(s) from page {pageNumber}");
    }

    /// <summary>
    /// MEMBER - Lấy tỉ lệ quy đổi tiền thành điểm
    /// </summary>
    [Authorize(Roles = "MEMBER, member")]
    [HttpGet("member/exchange-rate")]
    public async Task<IActionResult> GetMoneyToPointExchangeRate()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("User not authenticated");
        var exchangeRate = await _walletService.GetMoneyToPointExchangeRateAsync(userId.Value);
        return OkResponse(exchangeRate, "Retrieved money to point exchange rate successfully");
    }
}
