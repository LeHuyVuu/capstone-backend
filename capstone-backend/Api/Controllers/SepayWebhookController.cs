using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SepayWebhookController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SepayWebhookController> _logger;

    public SepayWebhookController(
        IUnitOfWork unitOfWork,
        ILogger<SepayWebhookController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint nhận callback từ Sepay khi có thanh toán
    /// </summary>
    /// <remarks>
    /// Sepay sẽ gọi endpoint này khi user chuyển khoản thành công.
    /// Endpoint này KHÔNG cần authentication.
    /// </remarks>
    [HttpPost("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromBody] SepayWebhookData webhook)
    {
        try
        {
            _logger.LogInformation("🔔 Sepay webhook received - OrderCode: {OrderCode}, Amount: {Amount}, Status: {Status}",
                webhook.OrderCode, webhook.Amount, webhook.Status);

            // 1. Validate webhook data
            if (string.IsNullOrEmpty(webhook.OrderCode))
            {
                _logger.LogWarning("⚠️ Invalid webhook - missing OrderCode");
                return BadRequest(new { message = "Invalid webhook data" });
            }

            // 2. Parse order code (format: VSP{subscriptionId})
            if (!webhook.OrderCode.StartsWith("VSP"))
            {
                _logger.LogWarning("⚠️ Invalid order code format: {OrderCode}", webhook.OrderCode);
                return BadRequest(new { message = "Invalid order code format" });
            }

            if (!int.TryParse(webhook.OrderCode.Substring(3), out int subscriptionId))
            {
                _logger.LogWarning("⚠️ Cannot parse subscription ID from: {OrderCode}", webhook.OrderCode);
                return BadRequest(new { message = "Invalid subscription ID" });
            }

            // 3. Load subscription and transaction
            var subscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Include(vsp => vsp.Package)
                .Include(vsp => vsp.Venue)
                .FirstOrDefaultAsync(vsp => vsp.Id == subscriptionId);

            if (subscription == null)
            {
                _logger.LogWarning("⚠️ Subscription not found: {Id}", subscriptionId);
                return NotFound(new { message = $"Subscription {subscriptionId} not found" });
            }

            var transaction = await _unitOfWork.Context.Set<Transaction>()
                .FirstOrDefaultAsync(t => t.TransType == 1 // VENUE_SUBSCRIPTION
                    && t.DocNo == subscriptionId);

            if (transaction == null)
            {
                _logger.LogWarning("⚠️ Transaction not found for subscription: {Id}", subscriptionId);
                return NotFound(new { message = $"Transaction for subscription {subscriptionId} not found" });
            }

            // 4. Check if already processed
            if (transaction.Status == "SUCCESS")
            {
                _logger.LogInformation("ℹ️ Transaction already processed: {Id}", transaction.Id);
                return Ok(new { message = "Transaction already processed" });
            }

            if (transaction.Status == "EXPIRED" || transaction.Status == "CANCELLED")
            {
                _logger.LogWarning("⚠️ Cannot process {Status} transaction: {Id}", 
                    transaction.Status, transaction.Id);
                return BadRequest(new { message = $"Transaction is {transaction.Status}" });
            }

            // 5. Validate amount
            if (webhook.Amount != (int)transaction.Amount)
            {
                _logger.LogError("❌ Amount mismatch - Expected: {Expected}, Received: {Received}",
                    transaction.Amount, webhook.Amount);
                return BadRequest(new { message = "Payment amount mismatch" });
            }

            // 6. Check payment status
            if (!string.Equals(webhook.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️ Payment not successful - Status: {Status}", webhook.Status);
                
                transaction.Status = "FAILED";
                transaction.UpdatedAt = DateTime.UtcNow;
                subscription.Status = "PAYMENT_FAILED";
                subscription.UpdatedAt = DateTime.UtcNow;
                
                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(new { message = "Payment failed, transaction status updated" });
            }

            // 7. Process successful payment
            using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            
            try
            {
                // 8. Update transaction with webhook data
                var externalRefData = !string.IsNullOrEmpty(transaction.ExternalRefCode)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.ExternalRefCode)
                    : new Dictionary<string, object>();

                if (externalRefData != null)
                {
                    externalRefData["sepayWebhookId"] = webhook.Id;
                    externalRefData["paidAt"] = DateTime.UtcNow;
                    externalRefData["transactionDate"] = webhook.TransactionDate ?? string.Empty;
                }

                transaction.Status = "SUCCESS";
                transaction.ExternalRefCode = JsonSerializer.Serialize(externalRefData);
                transaction.UpdatedAt = DateTime.UtcNow;

                // 9. Activate subscription
                var now = DateTime.UtcNow;
                var durationDays = subscription.Package.DurationDays ?? 30;
                var totalDays = durationDays * (subscription.Quantity ?? 1);

                subscription.Status = "ACTIVE";
                subscription.StartDate = now;
                subscription.EndDate = now.AddDays(totalDays);
                subscription.UpdatedAt = now;

                // 10. Update venue status to PENDING for admin approval
                if (subscription.Venue != null)
                {
                    subscription.Venue.Status = "PENDING";
                    subscription.Venue.UpdatedAt = now;
                    _unitOfWork.Context.Set<VenueLocation>().Update(subscription.Venue);
                }

                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                _logger.LogInformation("✅ Payment processed - TxId: {TxId}, SubId: {SubId}, VenueStatus: PENDING, Active until: {EndDate}",
                    transaction.Id, subscription.Id, subscription.EndDate);

                // TODO: Send notification to venue owner via SignalR/Push notification

                return Ok(new { 
                    message = "Payment processed successfully",
                    transactionId = transaction.Id,
                    subscriptionId = subscription.Id,
                    venueStatus = "PENDING"
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error processing webhook");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Webhook processing failed");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
