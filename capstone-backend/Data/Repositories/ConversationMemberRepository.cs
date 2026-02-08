using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for ConversationMember entity
/// </summary>
public class ConversationMemberRepository : GenericRepository<ConversationMember>, IConversationMemberRepository
{
    public ConversationMemberRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<ConversationMember>> GetActiveConversationMembersAsync(
        int conversationId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
            return new List<ConversationMember>();

        return await _context.ConversationMembers
            .Include(cm => cm.User)
            .Where(cm => cm.ConversationId == conversationId && cm.IsActive == true)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationMember?> GetMemberAsync(
        int conversationId, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || userId <= 0)
            return null;

        return await _context.ConversationMembers
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId 
                                    && cm.UserId == userId, 
                cancellationToken);
    }

    public async Task UpdateLastReadMessageAsync(
        int conversationId, 
        int userId, 
        int messageId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || userId <= 0 || messageId <= 0)
            return;

        var member = await GetMemberAsync(conversationId, userId, cancellationToken);
        if (member == null || member.IsActive != true)
            return;

        // Only update if new message is newer
        if (member.LastReadMessageId == null || messageId > member.LastReadMessageId)
        {
            member.LastReadMessageId = messageId;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetUnreadCountAsync(
        int conversationId, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || userId <= 0)
            return 0;

        var member = await GetMemberAsync(conversationId, userId, cancellationToken);
        if (member == null || member.IsActive != true)
            return 0;

        var unreadCount = await _context.Messages
            .Where(m => m.ConversationId == conversationId 
                     && m.IsDeleted == false
                     && m.SenderId != userId) // Don't count own messages
            .Where(m => member.LastReadMessageId == null || m.Id > member.LastReadMessageId)
            .CountAsync(cancellationToken);

        return unreadCount;
    }
}
