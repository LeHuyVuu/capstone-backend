using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Category;

public class UpdateCategoryRequest
{
    [MaxLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
    public string? Name { get; set; }


    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
