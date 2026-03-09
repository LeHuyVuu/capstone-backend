using System.Threading.Tasks;

namespace capstone_backend.Business.Interfaces
{
    public interface IInteractionTrackingService
    {
        /// <summary>
        /// Track user interaction with a target entity
        /// </summary>
        /// <param name="memberId">ID of the member performing the action</param>
        /// <param name="coupleId">Optional couple ID if action is performed as couple</param>
        /// <param name="interactionType">Type: VIEW, CLICK, FAVORITE, SHARE, etc.</param>
        /// <param name="targetType">Entity type: VenueLocation, CoupleMoodType, Advertisement, etc.</param>
        /// <param name="targetId">ID of the target entity</param>
        /// <param name="categoryInteraction">Category/theme of the target (e.g., Cafe, Restaurant, Bar)</param>
        Task TrackInteractionAsync(
            int memberId, 
            int? coupleId, 
            string interactionType, 
            string targetType, 
            int targetId,
            string? categoryInteraction = null);

        /// <summary>
        /// Track multiple interactions at once (batch operation)
        /// </summary>
        Task TrackBatchInteractionsAsync(
            int memberId,
            int? coupleId,
            string interactionType,
            string targetType,
            List<int> targetIds,
            string? categoryInteraction = null);
    }
}
