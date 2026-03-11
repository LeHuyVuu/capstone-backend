namespace capstone_backend.Business.DTOs.Category;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
