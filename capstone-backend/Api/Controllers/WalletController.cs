using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "VENUEOWNER")]
public class WalletController : BaseController
{
    private readonly WalletService _walletService;

    public WalletController(WalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>VENUE OWNER</summary>
    [HttpGet("balance")]
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

    /// <summary>VENUE OWNER</summary>
    [HttpPost("withdraw")]
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
}
