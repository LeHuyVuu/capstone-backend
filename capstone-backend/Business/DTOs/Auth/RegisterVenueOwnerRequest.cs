using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Auth;

/// <summary>
/// Request đăng ký VenueOwner (chủ địa điểm/doanh nghiệp)
/// </summary>
public class RegisterVenueOwnerRequest
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

    [Required(ErrorMessage = "Tên doanh nghiệp là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên doanh nghiệp không được quá 200 ký tự")]
    public string BusinessName { get; set; } = null!;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
    public string? Address { get; set; }
}
