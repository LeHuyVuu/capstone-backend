using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.Controllers
{
    [Route("api/withdraw-requests")]
    [ApiController]
    public class WithdrawRequestController : BaseController
    {
        private readonly IUnitOfWork _unitOfWork;

        public WithdrawRequestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get withdraw requests (filter by status if provided)
        /// </summary>
        /// <param name="status">PENDING | APPROVED | COMPLETED | REJECTED | CANCELLED</param>
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetWithdrawRequests([FromQuery] string? status)
        {
            try
            {
                WithdrawRequestStatus? parsedStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (!Enum.TryParse<WithdrawRequestStatus>(status, true, out var s))
                        return BadRequestResponse(
                            "Invalid status. Allowed: PENDING, APPROVED, COMPLETED, REJECTED, CANCELLED");

                    parsedStatus = s;
                }

                IQueryable<WithdrawRequest> query = _unitOfWork.Context.Set<WithdrawRequest>().AsNoTracking();

                if (parsedStatus.HasValue)
                {
                    var statusString = parsedStatus.Value.ToString();
                    query = query.Where(wr => wr.Status == statusString);
                }

                var entities = await query
                    .OrderByDescending(wr => wr.RequestedAt)
                    .ToListAsync();

                var result = entities.Select(wr =>
                {
                    BankInfoDto? bankInfo = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(wr.BankInfo))
                            bankInfo = System.Text.Json.JsonSerializer.Deserialize<BankInfoDto>(wr.BankInfo);
                    }
                    catch
                    {
                    }

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

                return OkResponse(result, $"Retrieved {result.Count} withdraw request(s)");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
