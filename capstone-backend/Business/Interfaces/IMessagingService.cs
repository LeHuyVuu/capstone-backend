using capstone_backend.Business.DTOs.Messaging;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface for messaging operations
/// </summary>
public interface IMessagingService
{
    /// <summary>
    /// Create a new conversation (direct or group)
    /// </summary>
    Task<ConversationResponse> CreateConversationAsync(int currentUserId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get or create direct conversation between two users
    /// </summary>
    Task<ConversationResponse> GetOrCreateDirectConversationAsync(int currentUserId, int otherUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all conversations for current user
    /// </summary>
    Task<List<ConversationResponse>> GetUserConversationsAsync(int currentUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get conversation by ID
    /// </summary>
    Task<ConversationResponse> GetConversationByIdAsync(int currentUserId, int conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a message
    /// </summary>
    Task<MessageResponse> SendMessageAsync(int currentUserId, SendMessageRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get messages in conversation with pagination
    /// </summary>
    Task<MessagesPageResponse> GetMessagesAsync(int currentUserId, int conversationId, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Mark message as read
    /// </summary>
    Task MarkAsReadAsync(int currentUserId, MarkAsReadRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add members to group conversation
    /// </summary>
    Task AddMembersAsync(int currentUserId, AddMembersRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove member from group conversation
    /// </summary>
    Task RemoveMemberAsync(int currentUserId, RemoveMemberRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Leave conversation
    /// </summary>
    Task LeaveConversationAsync(int currentUserId, int conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete message (soft delete)
    /// </summary>
    Task DeleteMessageAsync(int currentUserId, int messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search messages in conversation
    /// </summary>
    Task<List<MessageResponse>> SearchMessagesAsync(int currentUserId, int conversationId, string searchTerm, CancellationToken cancellationToken = default);
}
