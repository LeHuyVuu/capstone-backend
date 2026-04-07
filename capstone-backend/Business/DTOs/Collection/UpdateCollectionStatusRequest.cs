using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

/// <summary>
/// Request to update collection status (PUBLIC or PRIVATE)
/// </summary>
public class UpdateCollectionStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
}
