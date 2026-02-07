using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Represents a message in a conversation
/// </summary>
[Table("messages")]
public class Message
{
    [Key]
    [Column("id", TypeName = "integer")]
    public int Id { get; set; }
    
    [Column("conversation_id", TypeName = "integer")]
    public int? ConversationId { get; set; }
    
    [Column("sender_id", TypeName = "integer")]
    public int? SenderId { get; set; }
    
    [Column("content", TypeName = "text")]
    public string? Content { get; set; }
    
    [Column("message_type", TypeName = "text")]
    public string? MessageType { get; set; }
    
    [Column("reference_id", TypeName = "integer")]
    public int? ReferenceId { get; set; }
    
    [Column("reference_type", TypeName = "text")]
    public string? ReferenceType { get; set; }
    
    [Column("metadata", TypeName = "text")]
    public string? Metadata { get; set; }
    
    [Column("created_at", TypeName = "timestamptz")]
    public DateTime? CreatedAt { get; set; }
    
    [Column("updated_at", TypeName = "timestamptz")]
    public DateTime? UpdatedAt { get; set; }
    
    [Column("is_deleted", TypeName = "boolean")]
    public bool? IsDeleted { get; set; }
    
    // Navigation properties
    public virtual Conversation? Conversation { get; set; }
    public virtual UserAccount? Sender { get; set; }
}
