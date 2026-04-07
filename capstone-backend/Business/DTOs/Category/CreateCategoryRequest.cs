using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Category;

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
