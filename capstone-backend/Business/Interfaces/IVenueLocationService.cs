using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface for venue location operations
/// </summary>
public interface IVenueLocationService
{
    /// <summary>
    /// Get venue location detail by ID including location tag and venue owner profile
    /// </summary>
    /// <param name="venueId">The venue location ID</param>
    /// <returns>Venue location detail or null if not found</returns>
    Task<VenueLocationDetailResponse?> GetVenueLocationDetailByIdAsync(int venueId);

    /// <summary>
    /// Get reviews for a venue location with pagination
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Reviews with summary (average rating, total reviews, rating distribution) and paginated list</returns>
    Task<VenueReviewsWithSummaryResponse> GetReviewsByVenueIdAsync(int venueId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Get reviews for a venue location with optional date/month/year filter, sorted by time with review likes included (có phân trang)
    /// If no date filter provided, returns all reviews
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="date">Optional: Specific date to filter (yyyy-MM-dd)</param>
    /// <param name="month">Optional: Month to filter (1-12), requires year</param>
    /// <param name="year">Optional: Year to filter</param>
    /// <param name="sortDescending">Sort by created time descending (default: true)</param>
    /// <returns>Reviews with summary and paginated list including review likes</returns>
    Task<VenueReviewsWithSummaryResponse> GetReviewsWithLikesByVenueIdAsync(
        int venueId, 
        int page = 1, 
        int pageSize = 10, 
        DateTime? date = null,
        int? month = null, 
        int? year = null,
        bool sortDescending = true);

    /// <summary>
    /// Create a new venue location with location tags
    /// </summary>
    /// <param name="request">Create venue location request</param>
    /// <param name="userId">User ID - will resolve to venue owner profile</param>
    /// <returns>Created venue location response</returns>
    Task<VenueLocationCreateResponse> CreateVenueLocationAsync(CreateVenueLocationRequest request, int userId);

    /// <summary>
    /// Update venue location information
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="request">Update venue location request</param>
    /// <returns>Updated venue location response</returns>
    Task<VenueLocationDetailResponse?> UpdateVenueLocationAsync(int id, UpdateVenueLocationRequest request);

    /// <summary>
    /// Get all location tags with couple mood type and couple personality type
    /// </summary>
    /// <returns>List of location tags</returns>
    Task<List<LocationTagResponse>> GetAllLocationTagsAsync();

    /// <summary>
    /// Get all couple mood types
    /// </summary>
    /// <returns>List of couple mood types</returns>
    Task<List<CoupleMoodTypeInfo>> GetAllCoupleMoodTypesAsync();

    /// <summary>
    /// Get all couple personality types
    /// </summary>
    /// <returns>List of couple personality types</returns>
    Task<List<CouplePersonalityTypeInfo>> GetAllCouplePersonalityTypesAsync();

    /// <summary>
    /// Update venue opening hours for a specific day
    /// Automatically updates is_closed based on current time
    /// </summary>
    /// <param name="request">Update venue opening hour request</param>
    /// <returns>Updated venue opening hour response</returns>
    Task<VenueOpeningHourResponse?> UpdateVenueOpeningHourAsync(UpdateVenueOpeningHourRequest request);

    /// <summary>
    /// Automatically update IsClosed status for all venue opening hours based on current time
    /// This method is called by Hangfire as a recurring job every minute
    /// </summary>
    Task UpdateAllVenuesIsClosedStatusAsync();

    /// <summary>
    /// Get all venue locations for a venue owner by user ID
    /// Includes LocationTag details with CoupleMoodType and CouplePersonalityType
    /// </summary>
    /// <param name="userId">User ID from JWT token (sub claim)</param>
    /// <returns>List of venue locations with LocationTag details</returns>
    Task<List<VenueOwnerVenueLocationResponse>> GetVenueLocationsByVenueOwnerAsync(int userId);

    /// <summary>
    /// Submit venue location to admin for approval
    /// Validates required fields before changing status to PENDING
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="userId">User ID (owner)</param>
    /// <returns>Submission result with success status and missing fields if any</returns>
    Task<VenueSubmissionResult> SubmitVenueToAdminAsync(int venueId, int userId);
    
    /// <summary>
    /// Submit venue with payment - validates, creates subscription and transaction, generates QR code
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="userId">User ID (owner)</param>
    /// <param name="request">Payment request with packageId and quantity</param>
    /// <returns>Submission result with payment info and QR code</returns>
    Task<SubmitVenueWithPaymentResponse> SubmitVenueWithPaymentAsync(int venueId, int userId, SubmitVenueWithPaymentRequest request);
    
    /// <summary>
    /// Get pending venue locations for admin approval
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paged list of pending venues</returns>
    Task<PagedResult<VenueOwnerVenueLocationResponse>> GetPendingVenuesAsync(int page, int pageSize);
    /// <summary>
    /// Approve or reject a venue location
    /// </summary>
    /// <param name="request">Approval request containing venue ID and status</param>
    /// <returns>True if successful, False if failed</returns>
    Task<VenueSubmissionResult> ApproveVenueAsync(VenueApprovalRequest request);

    /// <summary>
    /// Delete (soft delete) a location tag from venue
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="locationTagId">Location tag ID to delete</param>
    /// <returns>True if successful, False if not found or cannot delete</returns>
    Task<bool> DeleteVenueLocationTagAsync(int venueId, int locationTagId);
}
