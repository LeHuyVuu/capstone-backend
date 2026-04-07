using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

public class CreateCollectionRequest
{
    [Required(ErrorMessage = "Tên bộ sưu tập là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên bộ sưu tập không được vượt quá 200 ký tự")]
    public string CollectionName { get; set; } = null!;
    
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }
    
    public string? Img { get; set; }
    
    [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
    public string? Status { get; set; } = "active";
}
