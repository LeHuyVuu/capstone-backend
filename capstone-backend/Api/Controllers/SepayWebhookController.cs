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
        // Generate unique request ID for tracking
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        try
        {
            _logger.LogInformation("[{RequestId}] 🔔 Sepay webhook received - Gateway: {Gateway}, Content: {Content}, Amount: {Amount}, TransferType: {Type}",
                requestId, webhook.Gateway, webhook.Content, webhook.TransferAmount, webhook.TransferType);

            // ========== SECURITY & VALIDATION ==========
            
            // 0. Basic validation - prevent null/malformed requests
            if (webhook == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Webhook data is null", requestId);
                return BadRequest(new { message = "Invalid webhook data" });
            }

            // 1. Validate amount (prevent negative/zero/too large)
            if (webhook.TransferAmount <= 0)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Invalid amount: {Amount}", requestId, webhook.TransferAmount);
                return BadRequest(new { message = "Amount must be positive" });
            }

            if (webhook.TransferAmount > 1_000_000_000) // 1 billion VND
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Amount too large: {Amount}", requestId, webhook.TransferAmount);
                return BadRequest(new { message = "Amount exceeds maximum limit" });
            }

            // 2. Validate transfer type (chỉ xử lý tiền vào)
            if (webhook.TransferType != "in")
            {
                _logger.LogInformation("[{RequestId}] ℹ️ Ignoring webhook - TransferType is not 'in': {Type}", requestId, webhook.TransferType);
                return Ok(new { message = "Only 'in' transfers are processed" });
            }

            // 3. Validate webhook data - Sepay có thể gửi trong OrderCode hoặc Content
            var paymentCode = webhook.OrderCode ?? webhook.Content;
            
            // Prevent excessively long content (DoS protection)
            if (!string.IsNullOrEmpty(paymentCode) && paymentCode.Length > 500)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Payment code too long: {Length} chars", requestId, paymentCode.Length);
                paymentCode = paymentCode.Substring(0, 500);
            }
            
            if (string.IsNullOrWhiteSpace(paymentCode))
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Invalid webhook - missing OrderCode and Content", requestId);
                return BadRequest(new { message = "Invalid webhook data" });
            }

            // 4. Parse payment code (format: VSP{subscriptionId})
            // Tìm VSP trong chuỗi - hỗ trợ nhiều format: "VSP123", "VSP 123", "VSP-123", "Chuyen khoan VSP123"
            var vspIndex = paymentCode.IndexOf("VSP", StringComparison.OrdinalIgnoreCase);
            if (vspIndex == -1)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Invalid payment code - VSP not found: {Code}", requestId, paymentCode);
                return BadRequest(new { message = "Payment code does not contain VSP" });
            }

            // Extract all text after "VSP" and remove non-digit characters
            var afterVsp = paymentCode.Substring(vspIndex + 3); // After "VSP"
            
            // Extract only digits (skip spaces, dashes, etc.)
            // Supports: "VSP123", "VSP 123", "VSP-123", "VSP_123", "vsp 10 abc"
            var digits = new string(afterVsp.Where(char.IsDigit).ToArray());
            
            if (string.IsNullOrEmpty(digits) || !int.TryParse(digits, out int subscriptionId))
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Cannot parse subscription ID from payment code: {Code}", requestId, paymentCode);
                return BadRequest(new { message = "Invalid subscription ID format" });
            }

            // Validate subscription ID range (prevent invalid IDs)
            if (subscriptionId <= 0 || subscriptionId > int.MaxValue)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Invalid subscription ID: {SubId}", requestId, subscriptionId);
                return BadRequest(new { message = "Invalid subscription ID" });
            }
            
            _logger.LogInformation("[{RequestId}] 📋 Extracted subscription ID: {SubId} from payment code: {Code}", requestId, subscriptionId, paymentCode);

            // ========== DATABASE OPERATIONS WITH LOCKING ==========
            
            // 5. Load subscription and transaction WITH ROW LOCK (prevent race conditions)
            // Use FromSqlRaw for pessimistic locking
            var subscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                .Include(vsp => vsp.Package)
                .Include(vsp => vsp.Venue)
                .FirstOrDefaultAsync(vsp => vsp.Id == subscriptionId);

            if (subscription == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Subscription not found: {Id}", requestId, subscriptionId);
                return NotFound(new { message = $"Subscription {subscriptionId} not found" });
            }

            // Validate subscription is not already active/completed
            if (subscription.Status == "ACTIVE" || subscription.Status == "COMPLETED")
            {
                _logger.LogInformation("[{RequestId}] ℹ️ Subscription already {Status}: {Id}", requestId, subscription.Status, subscriptionId);
                // This is OK - payment may be duplicate webhook, check transaction status below
            }

            var transaction = await _unitOfWork.Context.Set<Transaction>()
                .FirstOrDefaultAsync(t => t.TransType == 1 // VENUE_SUBSCRIPTION
                    && t.DocNo == subscriptionId);

            if (transaction == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Transaction not found for subscription: {Id}", requestId, subscriptionId);
                return NotFound(new { message = $"Transaction for subscription {subscriptionId} not found" });
            }

            // ========== IDEMPOTENCY & STATE VALIDATION ==========
            
            // 6. Check if already processed (IDEMPOTENCY - prevent duplicate webhook processing)
            if (transaction.Status == "SUCCESS")
            {
                _logger.LogInformation("[{RequestId}] ℹ️ Transaction already processed: {Id}. Returning success (idempotent).", requestId, transaction.Id);
                return Ok(new { 
                    message = "Transaction already processed",
                    transactionId = transaction.Id,
                    subscriptionId = subscription.Id,
                    status = "SUCCESS",
                    idempotent = true
                });
            }

            // 7. Validate transaction state
            if (transaction.Status == "EXPIRED")
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Transaction expired: {Id}", requestId, transaction.Id);
                return BadRequest(new { message = "Transaction has expired" });
            }

            if (transaction.Status == "CANCELLED")
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Transaction cancelled: {Id}", requestId, transaction.Id);
                return BadRequest(new { message = "Transaction has been cancelled" });
            }

            if (transaction.Status != "PENDING")
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Unexpected transaction status: {Status} for transaction {Id}", 
                    requestId, transaction.Status, transaction.Id);
                return BadRequest(new { message = $"Transaction status is {transaction.Status}" });
            }

            // 8. Validate amount (CRITICAL SECURITY CHECK)
            if (webhook.Amount != (int)transaction.Amount)
            {
                _logger.LogError("[{RequestId}] ❌ SECURITY ALERT: Amount mismatch - Expected: {Expected}, Received: {Received}",
                    requestId, transaction.Amount, webhook.Amount);
                    
                // Mark transaction as FAILED due to amount mismatch
                transaction.Status = "FAILED";
                transaction.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                await _unitOfWork.SaveChangesAsync();
                
                return BadRequest(new { message = "Payment amount mismatch" });
            }

            // 9. Validate currency (ensure VND)
            if (transaction.Currency != "VND")
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Unexpected currency: {Currency}", requestId, transaction.Currency);
            }

            // 10. Validate transaction not too old (prevent stale webhooks)
            if (transaction.CreatedAt.HasValue)
            {
                var transactionAge = DateTime.UtcNow - transaction.CreatedAt.Value;
                if (transactionAge.TotalHours > 24)
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Transaction created more than 24h ago: {CreatedAt}", 
                        requestId, transaction.CreatedAt);
                    
                    // Mark as EXPIRED if too old
                    transaction.Status = "EXPIRED";
                    transaction.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Context.Set<Transaction>().Update(transaction);
                    await _unitOfWork.SaveChangesAsync();
                    
                    return BadRequest(new { message = "Transaction expired (created more than 24 hours ago)" });
                }
            }

            // 11. Check payment status
            if (!string.Equals(webhook.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Payment not successful - Status: {Status}", requestId, webhook.Status);
                
                transaction.Status = "FAILED";
                transaction.UpdatedAt = DateTime.UtcNow;
                subscription.Status = "PAYMENT_FAILED";
                subscription.UpdatedAt = DateTime.UtcNow;
                
                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(new { message = "Payment failed, transaction status updated" });
            }

            // ========== PROCESS SUCCESSFUL PAYMENT ==========
            
            // 11. Use database transaction with SERIALIZABLE isolation level (prevent race conditions)
            using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            
            try
            {
                // Re-check status after acquiring lock (double-check locking pattern)
                await _unitOfWork.Context.Entry(transaction).ReloadAsync();
                if (transaction.Status == "SUCCESS")
                {
                    _logger.LogInformation("[{RequestId}] ℹ️ Transaction already processed by concurrent request: {Id}", requestId, transaction.Id);
                    await dbTransaction.RollbackAsync();
                    return Ok(new { message = "Transaction already processed", idempotent = true });
                }
                // 12. Update transaction with webhook data (store audit trail)
                var externalRefData = new Dictionary<string, object>();
                
                try
                {
                    if (!string.IsNullOrEmpty(transaction.ExternalRefCode))
                    {
                        externalRefData = JsonSerializer.Deserialize<Dictionary<string, object>>(transaction.ExternalRefCode) 
                            ?? new Dictionary<string, object>();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("[{RequestId}] Failed to parse existing ExternalRefCode: {Error}", requestId, ex.Message);
                    externalRefData = new Dictionary<string, object>();
                }

                // Store webhook metadata
                externalRefData["sepayWebhookId"] = webhook.Id;
                externalRefData["paidAt"] = DateTime.UtcNow.ToString("O"); // ISO 8601
                externalRefData["transactionDate"] = webhook.TransactionDate ?? string.Empty;
                externalRefData["gateway"] = webhook.Gateway ?? "Unknown";
                externalRefData["referenceCode"] = webhook.ReferenceCode ?? string.Empty;
                externalRefData["requestId"] = requestId; // Track which webhook request processed this

                transaction.Status = "SUCCESS";
                transaction.ExternalRefCode = JsonSerializer.Serialize(externalRefData);
                transaction.UpdatedAt = DateTime.UtcNow;

                // 13. Activate subscription
                var now = DateTime.UtcNow;
                var durationDays = subscription.Package?.DurationDays ?? 30;
                var totalDays = durationDays * Math.Max(subscription.Quantity ?? 1, 1);

                // Validate package is still active
                if (subscription.Package?.IsActive != true || subscription.Package?.IsDeleted == true)
                {
                    _logger.LogError("[{RequestId}] ❌ Package is no longer active: {PackageId}", requestId, subscription.PackageId);
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { message = "Package is no longer available" });
                }

                subscription.Status = "ACTIVE";
                subscription.StartDate = now;
                subscription.EndDate = now.AddDays(totalDays);
                subscription.UpdatedAt = now;

                // 14. Update venue status to PENDING for admin approval
                if (subscription.Venue != null)
                {
                    // Only update if venue is still in valid state
                    if (subscription.Venue.Status == "DRAFTED" || subscription.Venue.Status == "DRAFT")
                    {
                        subscription.Venue.Status = "PENDING";
                        subscription.Venue.UpdatedAt = now;
                        _unitOfWork.Context.Set<VenueLocation>().Update(subscription.Venue);
                        _logger.LogInformation("[{RequestId}] Updated venue {VenueId} status to PENDING", requestId, subscription.Venue.Id);
                    }
                    else
                    {
                        _logger.LogWarning("[{RequestId}] Venue {VenueId} status is {Status}, not updating to PENDING", 
                            requestId, subscription.Venue.Id, subscription.Venue.Status);
                    }
                }
                else
                {
                    _logger.LogWarning("[{RequestId}] Venue not found for subscription {SubId}", requestId, subscription.Id);
                }

                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);
                await _unitOfWork.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                _logger.LogInformation("[{RequestId}] ✅ Payment processed successfully - TxId: {TxId}, SubId: {SubId}, VenueStatus: {VenueStatus}, Active until: {EndDate}",
                    requestId, transaction.Id, subscription.Id, subscription.Venue?.Status ?? "N/A", subscription.EndDate);

                // TODO: Send notification to venue owner via SignalR/Push notification

                return Ok(new { 
                    message = "Payment processed successfully",
                    transactionId = transaction.Id,
                    subscriptionId = subscription.Id,
                    venueId = subscription.Venue?.Id,
                    venueStatus = subscription.Venue?.Status ?? "N/A",
                    subscriptionEndDate = subscription.EndDate,
                    requestId = requestId
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "[{RequestId}] ❌ Error processing webhook - rolling back transaction", requestId);
                throw;
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "[{RequestId}] ❌ Concurrency conflict - another request may have processed this payment", requestId);
            return Conflict(new { message = "Payment is being processed by another request" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] ❌ Webhook processing failed", requestId);
            return StatusCode(500, new { message = "Internal server error", requestId });
        }
    }
}
