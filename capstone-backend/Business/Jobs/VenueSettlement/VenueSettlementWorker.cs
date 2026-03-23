
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Jobs.VenueSettlement
{
    public class VenueSettlementWorker : IVenueSettlementWorker
    {
        private readonly ILogger<VenueSettlementWorker> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public VenueSettlementWorker(ILogger<VenueSettlementWorker> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessPendingSettlementsAsync()
        {
            var now = DateTime.UtcNow;
            var settlements = await _unitOfWork.VenueSettlements.GetDueSettlementsAsync(now);

            if (settlements == null || !settlements.Any())
            {
                _logger.LogInformation("No due venue settlements found at {Now}", now);
                return;
            }

            var venueOwnerIds = settlements.Select(s => s.VenueOwnerId).Distinct().ToList();
            var venueOwners = await _unitOfWork.VenueOwnerProfiles.GetByIdsAsync(venueOwnerIds);
            if (venueOwners == null || !venueOwners.Any())
            {
                _logger.LogWarning("No venue owners found for due settlements {Now}", now);
                return;
            }
            var venueOwnerDict = venueOwners.ToDictionary(vo => vo.Id, vo => vo);

            var userIds = venueOwners.Select(vo => vo.UserId).Distinct().ToList();

            // Get wallet of each venue owner
            var wallets = await _unitOfWork.Wallets.GetByUserIdsAsync(userIds);
            var walletDict = wallets.ToDictionary(w => w.UserId, w => w);

            var processedCount = 0;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var settlement in settlements)
                {
                    if (!venueOwnerDict.TryGetValue(settlement.VenueOwnerId, out var venueOwner))
                    {
                        _logger.LogWarning(
                            "Venue owner not found for settlement {SettlementId}, VenueOwnerId {VenueOwnerId}",
                            settlement.Id,
                            settlement.VenueOwnerId);
                        continue;
                    }

                    if (!walletDict.TryGetValue(venueOwner.UserId, out var wallet))
                    {
                        _logger.LogWarning(
                            "Wallet not found for venue owner {VenueOwnerId}, UserId {UserId}",
                            venueOwner.Id,
                            venueOwner.UserId);
                        continue;
                    }

                    // Update wallet balance
                    wallet.Balance += settlement.NetAmount;
                    wallet.UpdatedAt = now;
                    _unitOfWork.Wallets.Update(wallet);

                    // Create transaction record
                    var transaction = new Transaction
                    {
                        DocNo = settlement.Id,
                        Amount = settlement.NetAmount,
                        Currency = "VND",
                        UserId = venueOwner.UserId,
                        PaymentMethod = PaymentMethod.SYSTEM.ToString(),
                        TransType = 5,
                        Description = $"Settlement for VenueOwnerId {venueOwner.Id}, SettlementId {settlement.Id}",
                        ExternalRefCode = null,
                        Status = TransactionStatus.SUCCESS.ToString()
                    };

                    await _unitOfWork.Transactions.AddAsync(transaction);

                    // Update settlement status
                    settlement.Status = VenueSettlementStatus.PAID.ToString();
                    settlement.PaidAt = now;
                    settlement.UpdatedAt = now;

                    _unitOfWork.VenueSettlements.Update(settlement);

                    processedCount++;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                   "Processed {ProcessedCount}/{TotalCount} venue settlements successfully at {Now}",
                   processedCount,
                   settlements.Count(),
                   now);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError($"Error processing venue settlements: {ex.Message}");
                return;
            }
        }
    }
}
