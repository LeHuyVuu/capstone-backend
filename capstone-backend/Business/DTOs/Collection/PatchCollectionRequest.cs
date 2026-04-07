using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

public class PatchCollectionRequest
{
    [Required(ErrorMessage = "Danh sách ID địa điểm là bắt buộc")]
    public List<int> VenueIds { get; set; } = new();
}
