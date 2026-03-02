namespace capstone_backend.Business.DTOs.Collection;

/// <summary>
/// Simplified collection response for selection/dropdown purposes
/// </summary>
public class CollectionSummaryResponse
{
    public int Id { get; set; }
    public string? CollectionName { get; set; }
    public string? Description { get; set; }
    public string? Img { get; set; }
    public string? Status { get; set; }
}
