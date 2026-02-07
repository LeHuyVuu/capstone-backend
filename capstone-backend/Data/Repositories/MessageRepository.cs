using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for Message entity
/// </summary>
public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<Message>> GetConversationMessagesAsync(
        int conversationId, 
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || pageNumber <= 0 || pageSize <= 0)
            return new List<Message>();

        // Limit page size to prevent abuse
        pageSize = Math.Min(pageSize, 100);

        return await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && m.IsDeleted == false)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Message>> GetMessagesAfterAsync(
        int conversationId, 
        int afterMessageId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || afterMessageId < 0)
            return new List<Message>();

        return await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId 
                     && m.Id > afterMessageId 
                     && m.IsDeleted == false)
            .OrderBy(m => m.CreatedAt)
            .Take(100) // Limit to prevent abuse
            .ToListAsync(cancellationToken);
    }

    public async Task<Message?> GetByIdWithSenderAsync(
        int messageId, 
        CancellationToken cancellationToken = default)
    {
        if (messageId <= 0)
            return null;

        return await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.IsDeleted == false, cancellationToken);
    }

    public async Task<List<Message>> SearchMessagesAsync(
        int conversationId, 
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || string.IsNullOrWhiteSpace(searchTerm))
            return new List<Message>();

        searchTerm = searchTerm.Trim().ToLower();

        return await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId 
                     && m.IsDeleted == false
                     && m.Content != null 
                     && m.Content.ToLower().Contains(searchTerm))
            .OrderByDescending(m => m.CreatedAt)
            .Take(50) // Limit search results
            .ToListAsync(cancellationToken);
    }
}
