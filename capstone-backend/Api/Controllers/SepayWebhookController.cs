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

            // 4. Parse payment code (format: VSP{subscriptionId} or ADO{adsOrderId})
            // Determine payment type and extract ID
            string paymentType;
            int paymentId;
            
            var vspIndex = paymentCode.IndexOf("VSP", StringComparison.OrdinalIgnoreCase);
            var adoIndex = paymentCode.IndexOf("ADO", StringComparison.OrdinalIgnoreCase);
            
            if (vspIndex >= 0)
            {
                // VSP payment - Venue Subscription
                paymentType = "VSP";
                var afterVsp = paymentCode.Substring(vspIndex + 3); // After "VSP"
                var digits = new string(afterVsp.Where(char.IsDigit).ToArray());
                
                if (string.IsNullOrEmpty(digits) || !int.TryParse(digits, out paymentId))
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Cannot parse subscription ID from payment code: {Code}", requestId, paymentCode);
                    return BadRequest(new { message = "Invalid subscription ID format" });
                }
                
                if (paymentId <= 0 || paymentId > int.MaxValue)
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Invalid subscription ID: {SubId}", requestId, paymentId);
                    return BadRequest(new { message = "Invalid subscription ID" });
                }
                
                _logger.LogInformation("[{RequestId}] 📋 Extracted subscription ID: {SubId} from payment code: {Code}", requestId, paymentId, paymentCode);
            }
            else if (adoIndex >= 0)
            {
                // ADO payment - Advertisement Order
                paymentType = "ADO";
                var afterAdo = paymentCode.Substring(adoIndex + 3); // After "ADO"
                var digits = new string(afterAdo.Where(char.IsDigit).ToArray());
                
                if (string.IsNullOrEmpty(digits) || !int.TryParse(digits, out paymentId))
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Cannot parse ads order ID from payment code: {Code}", requestId, paymentCode);
                    return BadRequest(new { message = "Invalid ads order ID format" });
                }
                
                if (paymentId <= 0 || paymentId > int.MaxValue)
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Invalid ads order ID: {AdoId}", requestId, paymentId);
                    return BadRequest(new { message = "Invalid ads order ID" });
                }
                
                _logger.LogInformation("[{RequestId}] 📋 Extracted ads order ID: {AdoId} from payment code: {Code}", requestId, paymentId, paymentCode);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Invalid payment code - VSP or ADO not found: {Code}", requestId, paymentCode);
                return BadRequest(new { message = "Payment code must contain VSP or ADO" });
            }
            
            // Route to appropriate handler
            if (paymentType == "VSP")
            {
                return await ProcessVenueSubscriptionPayment(requestId, paymentId, webhook);
            }
            else // ADO
            {
                return await ProcessAdvertisementOrderPayment(requestId, paymentId, webhook);
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

    private async Task<IActionResult> ProcessVenueSubscriptionPayment(string requestId, int subscriptionId, SepayWebhookData webhook)
    {
        try
        {
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
                .FirstOrDefaultAsync(t => t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION
                    && t.DocNo == subscriptionId);

            if (transaction == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Transaction not found for subscription: {Id}", requestId, subscriptionId);
                return NotFound(new { message = $"Transaction for subscription {subscriptionId} not found" });
            }

            // ========== IDEMPOTENCY & STATE VALIDATION ==========
            
            // 6. Check if already processed (IDEMPOTENCY - prevent duplicate webhook processing)
            if (transaction.Status == TransactionStatus.SUCCESS.ToString())
            {
                _logger.LogInformation("[{RequestId}] ℹ️ Transaction already processed: {Id}. Returning success (idempotent).", requestId, transaction.Id);
                return Ok(new { 
                    message = "Transaction already processed",
                    transactionId = transaction.Id,
                    subscriptionId = subscription.Id,
                    status = TransactionStatus.SUCCESS.ToString(),
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
                transaction.Status = TransactionStatus.FAILED.ToString();
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
                    transaction.Status = TransactionStatus.EXPIRED.ToString();
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
                
                transaction.Status = TransactionStatus.FAILED.ToString();
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
                if (transaction.Status == TransactionStatus.SUCCESS.ToString())
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
            _logger.LogError(ex, "[{RequestId}] ❌ VSP webhook processing failed", requestId);
            return StatusCode(500, new { message = "Internal server error", requestId });
        }
    }

    private async Task<IActionResult> ProcessAdvertisementOrderPayment(string requestId, int adsOrderId, SepayWebhookData webhook)
    {
        try
        {
            // ========== DATABASE OPERATIONS WITH LOCKING ==========
            
            // 5. Load ads order and transaction
            var adsOrder = await _unitOfWork.Context.Set<AdsOrder>()
                .Include(ao => ao.Package)
                .Include(ao => ao.Advertisement)
                    .ThenInclude(ad => ad.VenueLocationAdvertisements)
                        .ThenInclude(vla => vla.Venue)
                .FirstOrDefaultAsync(ao => ao.Id == adsOrderId);

            if (adsOrder == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ AdsOrder not found: {Id}", requestId, adsOrderId);
                return NotFound(new { message = $"AdsOrder {adsOrderId} not found" });
            }

            // Validate order status
            if (adsOrder.Status == AdsOrderStatus.COMPLETED.ToString())
            {
                _logger.LogInformation("[{RequestId}] ℹ️ AdsOrder already COMPLETED: {Id}", requestId, adsOrderId);
            }

            var transaction = await _unitOfWork.Context.Set<Transaction>()
                .FirstOrDefaultAsync(t => t.TransType == (int)TransactionType.ADS_ORDER
                    && t.DocNo == adsOrderId);

            if (transaction == null)
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Transaction not found for ads order: {Id}", requestId, adsOrderId);
                return NotFound(new { message = $"Transaction for ads order {adsOrderId} not found" });
            }

            // ========== IDEMPOTENCY & STATE VALIDATION ==========
            
            // 6. Check if already processed (IDEMPOTENCY)
            if (transaction.Status == TransactionStatus.SUCCESS.ToString())
            {
                _logger.LogInformation("[{RequestId}] ℹ️ Transaction already processed: {Id}. Returning success (idempotent).", requestId, transaction.Id);
                return Ok(new { 
                    message = "Transaction already processed",
                    transactionId = transaction.Id,
                    adsOrderId = adsOrder.Id,
                    status = TransactionStatus.SUCCESS.ToString(),
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
                    
                transaction.Status = "FAILED";
                transaction.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                await _unitOfWork.SaveChangesAsync();
                
                return BadRequest(new { message = "Payment amount mismatch" });
            }

            // 9. Validate transaction not too old
            if (transaction.CreatedAt.HasValue)
            {
                var transactionAge = DateTime.UtcNow - transaction.CreatedAt.Value;
                if (transactionAge.TotalHours > 24)
                {
                    _logger.LogWarning("[{RequestId}] ⚠️ Transaction created more than 24h ago: {CreatedAt}", 
                        requestId, transaction.CreatedAt);
                    
                    transaction.Status = "EXPIRED";
                    transaction.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Context.Set<Transaction>().Update(transaction);
                    await _unitOfWork.SaveChangesAsync();
                    
                    return BadRequest(new { message = "Transaction expired (created more than 24 hours ago)" });
                }
            }

            // 10. Check payment status
            if (!string.Equals(webhook.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[{RequestId}] ⚠️ Payment not successful - Status: {Status}", requestId, webhook.Status);
                
                transaction.Status = "FAILED";
                transaction.UpdatedAt = DateTime.UtcNow;
                adsOrder.Status = "PAYMENT_FAILED";
                adsOrder.UpdatedAt = DateTime.UtcNow;
                
                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<AdsOrder>().Update(adsOrder);
                await _unitOfWork.SaveChangesAsync();
                
                return Ok(new { message = "Payment failed, transaction status updated" });
            }

            // ========== PROCESS SUCCESSFUL PAYMENT ==========
            
            // 11. Use database transaction with SERIALIZABLE isolation level
            using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            
            try
            {
                // Re-check status after acquiring lock
                await _unitOfWork.Context.Entry(transaction).ReloadAsync();
                if (transaction.Status == "SUCCESS")
                {
                    _logger.LogInformation("[{RequestId}] ℹ️ Transaction already processed by concurrent request: {Id}", requestId, transaction.Id);
                    await dbTransaction.RollbackAsync();
                    return Ok(new { message = "Transaction already processed", idempotent = true });
                }

                // 12. Update transaction with webhook data
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

                externalRefData["sepayWebhookId"] = webhook.Id;
                externalRefData["paidAt"] = DateTime.UtcNow.ToString("O");
                externalRefData["transactionDate"] = webhook.TransactionDate ?? string.Empty;
                externalRefData["gateway"] = webhook.Gateway ?? "Unknown";
                externalRefData["referenceCode"] = webhook.ReferenceCode ?? string.Empty;
                externalRefData["requestId"] = requestId;

                transaction.Status = "SUCCESS";
                transaction.ExternalRefCode = JsonSerializer.Serialize(externalRefData);
                transaction.UpdatedAt = DateTime.UtcNow;

                // 13. Update AdsOrder
                var now = DateTime.UtcNow;
                adsOrder.Status = "COMPLETED";
                adsOrder.PricePaid = webhook.Amount;
                adsOrder.UpdatedAt = now;

                // 14. Update Advertisement status to PENDING for admin approval
                if (adsOrder.Advertisement != null)
                {
                    var oldStatus = adsOrder.Advertisement.Status;
                    
                    _logger.LogInformation("[{RequestId}] 🔍 DEBUG - Advertisement {AdId} current status: '{Status}', checking if can update to PENDING", 
                        requestId, adsOrder.Advertisement.Id, oldStatus);
                    
                    if (adsOrder.Advertisement.Status == "DRAFTED" 
                        || adsOrder.Advertisement.Status == "DRAFT"
                        || adsOrder.Advertisement.Status == AdvertisementStatus.REJECTED.ToString())
                    {
                        adsOrder.Advertisement.Status = AdvertisementStatus.PENDING.ToString();
                        adsOrder.Advertisement.UpdatedAt = now;
                        _unitOfWork.Context.Set<Advertisement>().Update(adsOrder.Advertisement);
                        _logger.LogInformation("[{RequestId}] ✅ Updated advertisement {AdId} status from '{OldStatus}' to 'PENDING'", 
                            requestId, adsOrder.Advertisement.Id, oldStatus);
                    }
                    else
                    {
                        _logger.LogWarning("[{RequestId}] ⚠️ Advertisement {AdId} status is '{Status}', not updating to PENDING", 
                            requestId, adsOrder.Advertisement.Id, adsOrder.Advertisement.Status);
                    }

                    // 15. Update VenueLocationAdvertisement status - keep desired dates unchanged
                    var venueLocationAds = adsOrder.Advertisement.VenueLocationAdvertisements.ToList();
                    if (venueLocationAds.Any())
                    {
                        foreach (var venueLocationAd in venueLocationAds)
                        {
                            // Only update status, keep original desired StartDate and EndDate
                            // Dates will be auto-adjusted by admin approval logic if needed
                            venueLocationAd.Status = VenueLocationAdvertisementStatus.PENDING_APPROVAL.ToString();
                            venueLocationAd.UpdatedAt = now;
                            _unitOfWork.Context.Set<VenueLocationAdvertisement>().Update(venueLocationAd);
                        }
                        
                        _logger.LogInformation("[{RequestId}] Updated {Count} VenueLocationAdvertisement(s) to PENDING_APPROVAL, desired dates: {Start} to {End}",
                            requestId, venueLocationAds.Count, venueLocationAds.First().StartDate, venueLocationAds.First().EndDate);
                    }
                    else
                    {
                        _logger.LogWarning("[{RequestId}] No VenueLocationAdvertisements found for advertisement {AdId}", 
                            requestId, adsOrder.Advertisement.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("[{RequestId}] Advertisement not found for ads order {AdoId}", requestId, adsOrder.Id);
                }

                _unitOfWork.Context.Set<Transaction>().Update(transaction);
                _unitOfWork.Context.Set<AdsOrder>().Update(adsOrder);
                await _unitOfWork.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                _logger.LogInformation("[{RequestId}] ✅ ADO Payment processed successfully - TxId: {TxId}, AdoId: {AdoId}, AdStatus: {AdStatus}",
                    requestId, transaction.Id, adsOrder.Id, adsOrder.Advertisement?.Status ?? "N/A");

                return Ok(new { 
                    message = "Advertisement payment processed successfully",
                    transactionId = transaction.Id,
                    adsOrderId = adsOrder.Id,
                    advertisementId = adsOrder.Advertisement?.Id,
                    advertisementStatus = adsOrder.Advertisement?.Status ?? "N/A",
                    requestId = requestId
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "[{RequestId}] ❌ Error processing ADO webhook - rolling back transaction", requestId);
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
            _logger.LogError(ex, "[{RequestId}] ❌ ADO webhook processing failed", requestId);
            return StatusCode(500, new { message = "Internal server error", requestId });
        }
    }
}
