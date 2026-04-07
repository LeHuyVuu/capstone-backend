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
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        var balance = await _walletService.GetWalletBalanceAsync(userId.Value);

        if (balance == null)
            return NotFoundResponse("Không tìm thấy ví");

        return OkResponse(balance, "Lấy số dư ví thành công");
    }

    /// <summary>VENUE OWNER - Tạo giao dịch nạp tiền vào wallet qua VietQR (chỉ xử lý transaction)</summary>
    [HttpPost("topup")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> CreateTopupTransaction([FromBody] CreateWalletTopupRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        try
        {
            var result = await _walletService.CreateVenueOwnerWalletTopupAsync(userId.Value, request);
            return OkResponse(result, "Tạo giao dịch nạp tiền thành công");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>VENUE OWNER</summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "VENUEOWNER, MEMBER")]
    public async Task<IActionResult> CreateWithdrawRequest([FromBody] CreateWithdrawRequestRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        try
        {
            var withdrawRequest = await _walletService.CreateWithdrawRequestAsync(userId.Value, request);
            return CreatedResponse(withdrawRequest, "Tạo yêu cầu rút tiền thành công");
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
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        var requests = await _walletService.GetMyWithdrawRequestsAsync(userId.Value);
        return OkResponse(requests, $"Đã lấy {requests.Count} yêu cầu rút tiền");
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
            return UnauthorizedResponse("Người dùng chưa được xác thực");

        var history = await _walletService.GetWalletTransactionHistoryAsync(userId.Value, pageNumber, pageSize);
        return OkResponse(history, $"Đã lấy {history.Items.Count()} giao dịch ở trang {pageNumber}");
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
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        try
        {
            var result = await _walletService.ConvertMoneyToPointAsync(userId.Value, request);
            return OkResponse(result, "Quy đổi tiền sang điểm thành công");
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
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        var history = await _walletService.GetMemberWalletTransactionHistoryAsync(userId.Value, pageNumber, pageSize);
        return OkResponse(history, $"Đã lấy {history.Items.Count()} giao dịch ở trang {pageNumber}");
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
            return UnauthorizedResponse("Người dùng chưa được xác thực");
        var exchangeRate = await _walletService.GetMoneyToPointExchangeRateAsync(userId.Value);
        return OkResponse(exchangeRate, "Lấy tỷ lệ quy đổi tiền sang điểm thành công");
    }
}
