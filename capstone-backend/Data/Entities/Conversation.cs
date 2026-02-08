using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Represents a conversation between users
/// </summary>
[Table("conversations")]
public class Conversation
{
    [Key]
    [Column("id", TypeName = "integer")]
    public int Id { get; set; }
    
    [Column("type", TypeName = "text")]
    public string? Type { get; set; }
    
    [Column("name", TypeName = "text")]
    public string? Name { get; set; }
    
    [Column("created_by", TypeName = "integer")]
    public int? CreatedBy { get; set; }
    
    [Column("created_at", TypeName = "timestamptz")]
    public DateTime? CreatedAt { get; set; }
    
    [Column("is_deleted", TypeName = "boolean")]
    public bool? IsDeleted { get; set; }
    
    // Navigation properties
    public virtual UserAccount? Creator { get; set; }
    public virtual ICollection<ConversationMember>? Members { get; set; }
    public virtual ICollection<Message>? Messages { get; set; }
}
