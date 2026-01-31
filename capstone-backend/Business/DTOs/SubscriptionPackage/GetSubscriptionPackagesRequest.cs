using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SubscriptionPackage;

public class GetSubscriptionPackagesRequest
{
    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(MEMBER|VENUE)$", ErrorMessage = "Type must be either MEMBER or VENUE")]
    public string Type { get; set; } = string.Empty;
}
