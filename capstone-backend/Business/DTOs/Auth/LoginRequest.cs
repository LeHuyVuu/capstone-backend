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
    /// <example>minidora2707@gmail.com</example>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    /// <example>Nghia2707@</example>
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to keep the user logged in (persistent cookie)
    /// </summary>
    /// <example>true</example>
    public bool RememberMe { get; set; } = false;
}
