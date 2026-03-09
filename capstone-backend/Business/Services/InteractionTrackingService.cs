using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services
{
    public class InteractionTrackingService : IInteractionTrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InteractionTrackingService> _logger;

        public InteractionTrackingService(
            IUnitOfWork unitOfWork,
            ILogger<InteractionTrackingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task TrackInteractionAsync(
            int memberId,
            int? coupleId,
            string interactionType,
            string targetType,
            int targetId,
            string? categoryInteraction = null)
        {
            try
            {
                _logger.LogInformation("[SERVICE] 📝 TrackInteractionAsync called: Member {MemberId}, Type {InteractionType}, Target {TargetType} {TargetId}, Category {Category}",
                    memberId, interactionType, targetType, targetId, categoryInteraction ?? "(null)");
                
                // Validate interaction type
                var validInteractionTypes = new[] { "VIEW", "CLICK", "FAVORITE", "SHARE", "SAVE", "APPLY", "COMPLETE" };
                if (!validInteractionTypes.Contains(interactionType.ToUpper()))
                {
                    _logger.LogWarning("[SERVICE] ❌ Invalid interaction type: {InteractionType}", interactionType);
                    return;
                }

                // Create interaction record
                var interaction = new Interaction
                {
                    MemberId = memberId,
                    CoupleId = coupleId,
                    InteractionType = interactionType.ToUpper(),
                    TargetType = targetType,
                    TargetId = targetId,
                    CategoryInteraction = categoryInteraction,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("[SERVICE] 💾 Adding to DbContext...");
                await _unitOfWork.Context.Interactions.AddAsync(interaction);
                
                _logger.LogInformation("[SERVICE] 💾 Calling SaveChangesAsync...");
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "[SERVICE] ✅ SAVED to DB: Member {MemberId} {InteractionType} {TargetType} {TargetId}, Category: {Category}",
                    memberId, interactionType, targetType, targetId, categoryInteraction ?? "(null)");
            }
            catch (Exception ex)
            {
                // Don't throw - tracking failures shouldn't break user operations
                _logger.LogError(ex, 
                    "[SERVICE] ❌ EXCEPTION in TrackInteractionAsync: Member {MemberId} on {TargetType} {TargetId}",
                    memberId, targetType, targetId);
            }
        }

        public async Task TrackBatchInteractionsAsync(
            int memberId,
            int? coupleId,
            string interactionType,
            string targetType,
            List<int> targetIds,
            string? categoryInteraction = null)
        {
            try
            {
                var validInteractionTypes = new[] { "VIEW", "CLICK", "FAVORITE", "SHARE", "SAVE", "APPLY", "COMPLETE" };
                if (!validInteractionTypes.Contains(interactionType.ToUpper()))
                {
                    _logger.LogWarning("Invalid interaction type: {InteractionType}", interactionType);
                    return;
                }

                var interactions = targetIds.Select(targetId => new Interaction
                {
                    MemberId = memberId,
                    CoupleId = coupleId,
                    InteractionType = interactionType.ToUpper(),
                    TargetType = targetType,
                    TargetId = targetId,
                    CategoryInteraction = categoryInteraction,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _unitOfWork.Context.Interactions.AddRangeAsync(interactions);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Tracked {Count} batch interactions: Member {MemberId} {InteractionType} {TargetType}",
                    targetIds.Count, memberId, interactionType, targetType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to track batch interactions for Member {MemberId} on {TargetType}",
                    memberId, targetType);
            }
        }
    }
}
