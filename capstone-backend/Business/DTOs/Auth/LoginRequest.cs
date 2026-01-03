using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request model for user login
/// </summary>
/// <remarks>
/// Used for cookie-based authentication.
/// Both DataAnnotations and FluentValidation are applied for validation.
/// </remarks>
public class LoginRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>admin@example.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    /// <example>Password123!</example>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to keep the user logged in (persistent cookie)
    /// </summary>
    /// <example>true</example>
    public bool RememberMe { get; set; } = false;
}
