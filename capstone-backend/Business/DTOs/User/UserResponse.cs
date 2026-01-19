namespace capstone_backend.Business.DTOs.User;

/// <summary>
/// Response model for user data
/// </summary>
/// <remarks>
/// Used when returning user information.
/// Never includes sensitive data like password hash.
/// </remarks>
public class UserResponse
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Date when user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Date when user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date when user was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Member profile information (if user is a member)
    /// </summary>
    public MemberProfileResponse? MemberProfile { get; set; }

    /// <summary>
    /// Venue owner profile information (if user is a venue owner)
    /// </summary>
    public VenueOwnerProfileResponse? VenueOwnerProfile { get; set; }
}
