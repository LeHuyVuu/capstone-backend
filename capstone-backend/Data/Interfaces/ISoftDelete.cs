namespace capstone_backend.Data.Interfaces
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
