using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// User service interface for business logic
/// </summary>
/// <remarks>
/// Handles user-related business operations including authentication,
/// CRUD operations, and user management.
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Authenticate user and return user information
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with user info if successful, null if failed</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Register new user account
    /// Creates user account and corresponding profile (Member or VenueOwner)
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with user info and JWT tokens</returns>
    Task<LoginResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Register new VenueOwner account
    /// Creates user account with role="venueowner" and venue_owner_profile
    /// </summary>
    /// <param name="request">VenueOwner registration information</param>
    /// <returns>Login response with user info and JWT tokens</returns>
    Task<LoginResponse> RegisterVenueOwnerAsync(RegisterVenueOwnerRequest request);

    /// <summary>
    /// Get current user information by user ID
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User response if found, null otherwise</returns>
    Task<UserResponse?> GetCurrentUserAsync(int userId);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User response if found, null otherwise</returns>
    Task<UserResponse?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term for email or name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of users</returns>
    Task<PagedResult<UserResponse>> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null);

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="request">User creation data</param>
    /// <param name="createdBy">User ID who is creating this user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user response</returns>
    Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        int? createdBy = null);

    /// <summary>
    /// Update an existing user
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">Update data</param>
    /// <param name="updatedBy">User ID who is updating this user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user response if successful, null if user not found</returns>
    Task<UserResponse?> UpdateUserAsync(
        int userId,
        UpdateUserRequest request,
        int? updatedBy = null);

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="deletedBy">User ID who is deleting this user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if user not found</returns>
    Task<bool> DeleteUserAsync(
        int userId);
}
