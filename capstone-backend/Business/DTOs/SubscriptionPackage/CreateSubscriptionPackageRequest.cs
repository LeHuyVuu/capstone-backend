using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SubscriptionPackage;

public class CreateSubscriptionPackageRequest
{
    [Required(ErrorMessage = "Package name is required")]
    [StringLength(200, ErrorMessage = "Package name cannot exceed 200 characters")]
    public string PackageName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Duration days is required")]
    [Range(1, 3650, ErrorMessage = "Duration must be between 1 and 3650 days")]
    public int DurationDays { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringLength(30, ErrorMessage = "Type cannot exceed 30 characters")]
    public string Type { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
