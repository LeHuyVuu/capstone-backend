using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Messaging;

/// <summary>
/// DTO for creating a new conversation
/// </summary>
public class CreateConversationRequest
{
    [Required(ErrorMessage = "Conversation type is required")]
    public string Type { get; set; } = string.Empty;
    
    public string? Name { get; set; }
    
    [Required(ErrorMessage = "At least one member is required")]
    [MinLength(1, ErrorMessage = "At least one member is required")]
    public List<int> MemberIds { get; set; } = new();
}

/// <summary>
/// DTO for sending a message
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "Conversation ID is required")]
    public int ConversationId { get; set; }
    
    public string? Content { get; set; }
    
    public string MessageType { get; set; } = "TEXT"; // TEXT, IMAGE, FILE, VIDEO, AUDIO
    
    public int? ReferenceId { get; set; }
    
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// File URL (sau khi upload qua /api/messaging/upload-attachment)
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// File name gốc
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long? FileSize { get; set; }
    
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for marking message as read
/// </summary>
public class MarkAsReadRequest
{
    [Required(ErrorMessage = "Conversation ID is required")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Message ID is required")]
    public int MessageId { get; set; }
}

/// <summary>
/// DTO for typing indicator
/// </summary>
public class TypingIndicatorRequest
{
    [Required(ErrorMessage = "Conversation ID is required")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Is typing status is required")]
    public bool IsTyping { get; set; }
}

/// <summary>
/// DTO for adding members to group
/// </summary>
public class AddMembersRequest
{
    [Required(ErrorMessage = "Conversation ID is required")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "At least one member is required")]
    [MinLength(1, ErrorMessage = "At least one member is required")]
    public List<int> MemberIds { get; set; } = new();
}

/// <summary>
/// DTO for removing member from group
/// </summary>
public class RemoveMemberRequest
{
    [Required(ErrorMessage = "Conversation ID is required")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Member ID is required")]
    public int MemberId { get; set; }
}
