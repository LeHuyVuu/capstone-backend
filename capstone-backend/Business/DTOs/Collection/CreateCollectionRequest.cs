using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

public class CreateCollectionRequest
{
    [Required(ErrorMessage = "Collection name is required")]
    [StringLength(200, ErrorMessage = "Collection name cannot exceed 200 characters")]
    public string CollectionName { get; set; } = null!;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    public string? Img { get; set; }
    
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string? Status { get; set; } = "active";
}
