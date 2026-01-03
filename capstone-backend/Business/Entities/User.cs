using System.ComponentModel.DataAnnotations;
using capstone_backend.Business.Entities.Base;

namespace capstone_backend.Business.Entities;

/// <summary>
/// User entity representing application users
/// </summary>
/// <remarks>
/// Stores user information including authentication credentials.
/// Password should be hashed before storing (use BCrypt, Argon2, or similar).
/// </remarks>
public class User : BaseEntity
{
    /// <summary>
    /// User's email address (unique identifier for login)
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's hashed password
    /// </summary>
    /// <remarks>
    /// NEVER store plain text passwords. Always hash with BCrypt or Argon2.
    /// </remarks>
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number (optional)
    /// </summary>
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's role (e.g., Admin, User, Manager)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when user last logged in (UTC)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
