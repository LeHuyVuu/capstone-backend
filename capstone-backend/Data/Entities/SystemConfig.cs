namespace capstone_backend.Data.Entities
{
    public partial class SystemConfig
    {
        public int Id { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
