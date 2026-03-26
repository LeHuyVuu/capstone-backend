using capstone_backend.Business.DTOs.Wallet;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Email;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Common;
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
        private readonly IEmailService _emailService;

        public WithdrawRequestController(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        /// <summary>
        /// Get withdraw requests (filter by status if provided)
        /// </summary>
        /// <param name="status">PENDING | APPROVED | COMPLETED | REJECTED | CANCELLED</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetWithdrawRequests(
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

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

                var totalCount = await query.CountAsync();

                var entities = await query
                    .OrderByDescending(wr => wr.RequestedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var items = entities.Select(wr =>
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

                var result = new PagedResult<WithdrawRequestResponse>(items, pageNumber, pageSize, totalCount);

                return OkResponse(result, $"Retrieved {items.Count} withdraw request(s)");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Reject withdraw request (ADMIN)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{withdrawRequestId:int}/reject")]
        public async Task<IActionResult> RejectWithdrawRequest(int withdrawRequestId, [FromBody] RejectWithdrawRequestRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequestResponse("Reject reason is required");

                var withdrawRequest = await _unitOfWork.WithdrawRequests.GetByIdAsync(withdrawRequestId);
                if (withdrawRequest == null)
                    return NotFoundResponse("Withdraw request not found");

                var currentStatus = withdrawRequest.Status ?? WithdrawRequestStatus.PENDING.ToString();
                if (!string.Equals(currentStatus, WithdrawRequestStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase))
                    return BadRequestResponse($"Cannot reject withdraw request in '{currentStatus}' status. Only PENDING can be rejected");

                withdrawRequest.Status = WithdrawRequestStatus.REJECTED.ToString();
                withdrawRequest.RejectionReason = request.Reason.Trim();
                withdrawRequest.ProofImageUrl = null;
                withdrawRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.WithdrawRequests.Update(withdrawRequest);
                await _unitOfWork.SaveChangesAsync();

                return OkResponse(MapWithdrawRequestResponse(withdrawRequest), "Withdraw request rejected successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Approve withdraw request (ADMIN) - only change status to APPROVED
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{withdrawRequestId:int}/approve")]
        public async Task<IActionResult> ApproveWithdrawRequest(int withdrawRequestId)
        {
            try
            {
                var withdrawRequest = await _unitOfWork.Context.Set<WithdrawRequest>()
                    .Include(wr => wr.Wallet)
                    .ThenInclude(w => w.User)
                    .FirstOrDefaultAsync(wr => wr.Id == withdrawRequestId);

                if (withdrawRequest == null)
                    return NotFoundResponse("Withdraw request not found");

                var currentStatus = withdrawRequest.Status ?? WithdrawRequestStatus.PENDING.ToString();
                if (!string.Equals(currentStatus, WithdrawRequestStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase))
                    return BadRequestResponse($"Cannot approve withdraw request in '{currentStatus}' status. Only PENDING can be approved");

                withdrawRequest.Status = WithdrawRequestStatus.APPROVED.ToString();
                withdrawRequest.RejectionReason = null;
                withdrawRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.WithdrawRequests.Update(withdrawRequest);
                await _unitOfWork.SaveChangesAsync();

                // Gửi email thông báo approve
                try
                {
                    var user = withdrawRequest.Wallet?.User;
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        BankInfoDto? bankInfo = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(withdrawRequest.BankInfo))
                                bankInfo = System.Text.Json.JsonSerializer.Deserialize<BankInfoDto>(withdrawRequest.BankInfo);
                        }
                        catch { }

                        if (bankInfo != null)
                        {
                            var userName = user.DisplayName ?? user.Email;
                            var amount = withdrawRequest.Amount ?? 0;

                            var htmlBody = EmailApproveWithdrawTemplate.GetApproveWithdrawEmailContent(
                                userName,
                                amount,
                                bankInfo.BankName ?? "",
                                bankInfo.AccountNumber ?? "",
                                bankInfo.AccountName ?? ""
                            );

                            var textBody = EmailApproveWithdrawTemplate.GetApproveWithdrawPlainText(
                                userName,
                                amount,
                                bankInfo.BankName ?? "",
                                bankInfo.AccountNumber ?? "",
                                bankInfo.AccountName ?? ""
                            );

                            var emailRequest = new SendEmailRequest
                            {
                                To = user.Email,
                                Subject = "Yêu cầu rút tiền đã được phê duyệt - CoupleMood",
                                HtmlBody = htmlBody,
                                TextBody = textBody,
                                FromName = "CoupleMood"
                            };

                            await _emailService.SendEmailAsync(emailRequest);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    // Log lỗi nhưng không fail request
                    Console.WriteLine($"[WARNING] Failed to send approve withdraw email: {emailEx.Message}");
                }

                return OkResponse(MapWithdrawRequestResponse(withdrawRequest), "Withdraw request approved successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update withdraw request to COMPLETED with proof image (ADMIN)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{withdrawRequestId:int}/complete")]
        public async Task<IActionResult> CompleteWithdrawRequest(int withdrawRequestId, [FromBody] CompleteWithdrawRequestRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequestResponse("Request body is required");

                if (string.IsNullOrWhiteSpace(request.Status))
                    return BadRequestResponse("Status is required");

                if (!Enum.TryParse<WithdrawRequestStatus>(request.Status, true, out var targetStatus) ||
                    targetStatus != WithdrawRequestStatus.COMPLETED)
                    return BadRequestResponse("Invalid status. This API only allows status COMPLETED");

                if (string.IsNullOrWhiteSpace(request.ProofImageUrl))
                    return BadRequestResponse("Proof image URL is required");

                await _unitOfWork.BeginTransactionAsync();

                var withdrawRequest = await _unitOfWork.Context.Set<WithdrawRequest>()
                    .Include(wr => wr.Wallet)
                    .FirstOrDefaultAsync(wr => wr.Id == withdrawRequestId);

                if (withdrawRequest == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return NotFoundResponse("Withdraw request not found");
                }

                var currentStatusText = withdrawRequest.Status ?? WithdrawRequestStatus.PENDING.ToString();
                if (!Enum.TryParse<WithdrawRequestStatus>(currentStatusText, true, out var currentStatus))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse($"Current withdraw request status '{currentStatusText}' is invalid");
                }

                if (currentStatus != WithdrawRequestStatus.APPROVED)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse($"Cannot complete withdraw request in '{currentStatus}' status. Only APPROVED can be completed");
                }

                if (withdrawRequest.Amount == null || withdrawRequest.Amount <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse("Withdraw request amount is invalid");
                }

                var wallet = withdrawRequest.Wallet;
                if (wallet == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse("Wallet not found for this withdraw request");
                }

                if (wallet.IsActive != true)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse("Wallet is not active");
                }

                var currentBalance = wallet.Balance ?? 0;
                if (currentBalance < withdrawRequest.Amount.Value)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BadRequestResponse(
                        $"Insufficient balance to complete withdraw request. Available: {currentBalance:N0} VND");
                }

                wallet.Balance = currentBalance - withdrawRequest.Amount.Value;
                wallet.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Wallets.Update(wallet);

                withdrawRequest.Status = WithdrawRequestStatus.COMPLETED.ToString();
                withdrawRequest.ProofImageUrl = request.ProofImageUrl.Trim();
                withdrawRequest.RejectionReason = null;
                withdrawRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.WithdrawRequests.Update(withdrawRequest);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return OkResponse(MapWithdrawRequestResponse(withdrawRequest),
                    "Withdraw request completed successfully");
            }
            catch (Exception ex)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch
                {
                }

                return BadRequestResponse(ex.Message);
            }
        }


        private static WithdrawRequestResponse MapWithdrawRequestResponse(WithdrawRequest wr)
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
        }
    }
}
