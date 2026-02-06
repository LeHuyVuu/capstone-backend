using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class CoupleInvitationRepository : ICoupleInvitationRepository
{
    private readonly MyDbContext _context;

    public CoupleInvitationRepository(MyDbContext context)
    {
        _context = context;
    }

    public async Task<CoupleInvitation?> GetByIdAsync(int id)
    {
        return await _context.CoupleInvitations
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.IsDeleted == false);
    }

    public async Task<CoupleInvitation?> GetByIdWithMembersAsync(int id)
    {
        return await _context.CoupleInvitations
            .Include(ci => ci.SenderMember)
            .Include(ci => ci.ReceiverMember)
            .FirstOrDefaultAsync(ci => ci.Id == id && ci.IsDeleted == false);
    }

    public async Task<List<CoupleInvitation>> GetReceivedInvitationsAsync(int memberId, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.CoupleInvitations
            .Include(ci => ci.SenderMember)
            .Where(ci => ci.ReceiverMemberId == memberId && ci.IsDeleted == false);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ci => ci.Status == status);
        }

        return await query
            .OrderByDescending(ci => ci.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<CoupleInvitation>> GetSentInvitationsAsync(int memberId, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.CoupleInvitations
            .Include(ci => ci.ReceiverMember)
            .Where(ci => ci.SenderMemberId == memberId && ci.IsDeleted == false);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ci => ci.Status == status);
        }

        return await query
            .OrderByDescending(ci => ci.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> HasPendingInvitationBetweenAsync(int memberId1, int memberId2)
    {
        return await _context.CoupleInvitations
            .AnyAsync(ci =>
                ((ci.SenderMemberId == memberId1 && ci.ReceiverMemberId == memberId2) ||
                 (ci.SenderMemberId == memberId2 && ci.ReceiverMemberId == memberId1)) &&
                ci.Status == "PENDING" &&
                ci.IsDeleted == false);
    }

    public async Task<int> CountReceivedInvitationsAsync(int memberId, string? status = null)
    {
        var query = _context.CoupleInvitations
            .Where(ci => ci.ReceiverMemberId == memberId && ci.IsDeleted == false);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ci => ci.Status == status);
        }

        return await query.CountAsync();
    }

    public async Task<int> CountSentInvitationsAsync(int memberId, string? status = null)
    {
        var query = _context.CoupleInvitations
            .Where(ci => ci.SenderMemberId == memberId && ci.IsDeleted == false);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ci => ci.Status == status);
        }

        return await query.CountAsync();
    }

    public async Task AddAsync(CoupleInvitation invitation)
    {
        await _context.CoupleInvitations.AddAsync(invitation);
    }

    public async Task UpdateAsync(CoupleInvitation invitation)
    {
        _context.CoupleInvitations.Update(invitation);
        await Task.CompletedTask;
    }

    public async Task CancelAllPendingInvitationsForMemberAsync(int memberId)
    {
        var pendingInvitations = await _context.CoupleInvitations
            .Where(ci =>
                (ci.SenderMemberId == memberId || ci.ReceiverMemberId == memberId) &&
                ci.Status == "PENDING" &&
                ci.IsDeleted == false)
            .ToListAsync();

        foreach (var invitation in pendingInvitations)
        {
            invitation.Status = "CANCELLED";
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
