using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.User;

/// <summary>
/// Request model for updating an existing user
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// User's full name
    /// </summary>
    /// <example>Jane Doe</example>
    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number (optional)
    /// </summary>
    /// <example>+84987654321</example>
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's role
    /// </summary>
    /// <example>Admin</example>
    public string? Role { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    /// <example>true</example>
    public bool IsActive { get; set; } = true;
}
