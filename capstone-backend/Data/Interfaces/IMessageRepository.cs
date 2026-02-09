using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Repository interface for Message entity
/// </summary>
public interface IMessageRepository : IGenericRepository<Message>
{
    /// <summary>
    /// Get messages in a conversation with pagination
    /// </summary>
    Task<List<Message>> GetConversationMessagesAsync(
        int conversationId, 
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get messages after a specific message ID
    /// </summary>
    Task<List<Message>> GetMessagesAfterAsync(
        int conversationId, 
        int afterMessageId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get message by ID with sender info
    /// </summary>
    Task<Message?> GetByIdWithSenderAsync(int messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search messages in conversation
    /// </summary>
    Task<List<Message>> SearchMessagesAsync(
        int conversationId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);
}
