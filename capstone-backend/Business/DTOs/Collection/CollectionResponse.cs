using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.DTOs.Collection;

public class CollectionResponse
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? CollectionName { get; set; }
    public string? Description { get; set; }
    public string? Img { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public List<VenueSimpleResponse>? Venues { get; set; }
}

public class VenueSimpleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Address { get; set; } = null!;
}
