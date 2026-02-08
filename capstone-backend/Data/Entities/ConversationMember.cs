using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Represents a member in a conversation
/// </summary>
[Table("conversation_members")]
public class ConversationMember
{
    [Key]
    [Column("id", TypeName = "integer")]
    public int Id { get; set; }
    
    [Column("conversation_id", TypeName = "integer")]
    public int? ConversationId { get; set; }
    
    [Column("user_id", TypeName = "integer")]
    public int? UserId { get; set; }
    
    [Column("role", TypeName = "text")]
    public string? Role { get; set; }
    
    [Column("last_read_message_id", TypeName = "integer")]
    public int? LastReadMessageId { get; set; }
    
    [Column("joined_at", TypeName = "timestamptz")]
    public DateTime? JoinedAt { get; set; }
    
    [Column("is_active", TypeName = "boolean")]
    public bool? IsActive { get; set; }
    
    // Navigation properties
    public virtual Conversation? Conversation { get; set; }
    public virtual UserAccount? User { get; set; }
    public virtual Message? LastReadMessage { get; set; }
}
