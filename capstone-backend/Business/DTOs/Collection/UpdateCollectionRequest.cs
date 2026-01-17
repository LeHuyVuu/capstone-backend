using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

public class UpdateCollectionRequest
{
    [StringLength(200, ErrorMessage = "Collection name cannot exceed 200 characters")]
    public string? CollectionName { get; set; }
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string? Status { get; set; }
}
