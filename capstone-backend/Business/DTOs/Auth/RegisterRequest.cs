using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request đăng ký Member (người dùng thông thường)
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
    public string FullName { get; set; } = null!;

    // Optional fields
    public string? PhoneNumber { get; set; }
    
    public DateOnly? DateOfBirth { get; set; }
    
    /// <summary>
    /// Giới tính: "NAM", "NỮ", "KHÁC"
    /// </summary>
    public string? Gender { get; set; }
}
