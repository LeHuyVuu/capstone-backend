using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Category;

public class UpdateCategoryRequest
{
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
    public string? Name { get; set; }


    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
