using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Collection;

public class PatchCollectionRequest
{
    [Required(ErrorMessage = "Venue IDs are required")]
    public List<int> VenueIds { get; set; } = new();
}
