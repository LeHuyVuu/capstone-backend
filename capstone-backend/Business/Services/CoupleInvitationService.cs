using capstone_backend.Business.DTOs.CoupleInvitation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using capstone_backend.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class CoupleInvitationService : ICoupleInvitationService
{
    private readonly IUnitOfWork _unitOfWork;

    public CoupleInvitationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, string Message, CoupleInvitationResponse? Data)> SendInvitationDirectAsync(
        int senderMemberId,
        SendInvitationDirectRequest request)
    {
        // Cannot send to yourself
        if (request.ReceiverMemberId == senderMemberId)
        {
            return (false, "Không thể gửi lời mời cho chính mình", null);
        }

        // Check if receiver exists
        var receiver = await _unitOfWork.MembersProfile.GetByIdAsync(request.ReceiverMemberId);
        if (receiver == null)
        {
            return (false, "Không tìm thấy member này", null);
        }

        // Send invitation (without invite code)
        return await SendInvitationInternalAsync(senderMemberId, request.ReceiverMemberId, null, request.Message);
    }

    private async Task<(bool Success, string Message, CoupleInvitationResponse? Data)> SendInvitationInternalAsync(
        int senderMemberId,
        int receiverMemberId,
        string? inviteCodeUsed,
        string? message)
    {
        // Get sender
        var sender = await _unitOfWork.MembersProfile.GetByIdAsync(senderMemberId);
        if (sender == null || sender.IsDeleted == true)
        {
            return (false, "Không tìm thấy thông tin của bạn", null);
        }

        // Get receiver
        var receiver = await _unitOfWork.MembersProfile.GetByIdAsync(receiverMemberId);
        if (receiver == null || receiver.IsDeleted == true)
        {
            return (false, "Không tìm thấy member này", null);
        }

        // Edge case 1: Sender must be SINGLE
        if (sender.RelationshipStatus != "SINGLE")
        {
            return (false, "Bạn phải ở trạng thái SINGLE để gửi lời mời ghép đôi", null);
        }

        // Edge case 2: Receiver must be SINGLE
        if (receiver.RelationshipStatus != "SINGLE")
        {
            return (false, $"{receiver.FullName} không ở trạng thái SINGLE", null);
        }

        // Edge case 3: Sender must not have active couple profile
        var senderHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(senderMemberId);
        if (senderHasCouple != null)
        {
            return (false, "Bạn đã có cặp đôi rồi", null);
        }

        // Edge case 4: Receiver must not have active couple profile
        var receiverHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(receiverMemberId);
        if (receiverHasCouple != null)
        {
            return (false, $"{receiver.FullName} đã có cặp đôi rồi", null);
        }

        // Edge case 5: No pending invitation between them
        var hasPending = await _unitOfWork.CoupleInvitations.HasPendingInvitationBetweenAsync(senderMemberId, receiverMemberId);
        if (hasPending)
        {
            return (false, "Đã có lời mời đang chờ giữa 2 bạn rồi", null);
        }

        // Create invitation
        var invitation = new CoupleInvitation
        {
            SenderMemberId = senderMemberId,
            ReceiverMemberId = receiverMemberId,
            InviteCodeUsed = inviteCodeUsed,
            Status = "PENDING",
            Message = message,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.CoupleInvitations.AddAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

        // TODO: Send push notification to receiver

        var response = new CoupleInvitationResponse
        {
            InvitationId = invitation.Id,
            SenderMemberId = senderMemberId,
            SenderName = sender.FullName ?? "Unknown",
            SenderAvatarUrl = sender.User?.AvatarUrl,
            ReceiverMemberId = receiverMemberId,
            ReceiverName = receiver.FullName ?? "Unknown",
            ReceiverAvatarUrl = receiver.User?.AvatarUrl,
            InviteCodeUsed = inviteCodeUsed,
            Status = invitation.Status,
            Message = message,
            SentAt = invitation.SentAt,
            RespondedAt = null
        };

        return (true, "Đã gửi lời mời ghép đôi thành công", response);
    }

    public async Task<(bool Success, string Message, AcceptInvitationResponse? Data)> AcceptInvitationAsync(
        int invitationId,
        int currentMemberId)
    {
        // Get invitation with members
        var invitation = await _unitOfWork.CoupleInvitations.GetByIdWithMembersAsync(invitationId);
        if (invitation == null)
        {
            return (false, "Không tìm thấy lời mời này", null);
        }

        // Edge case 1: Only receiver can accept
        if (invitation.ReceiverMemberId != currentMemberId)
        {
            return (false, "Bạn không có quyền chấp nhận lời mời này", null);
        }

        // Edge case 2: Cannot accept if already accepted
        if (invitation.Status == "ACCEPTED")
        {
            return (false, "Lời mời này đã được chấp nhận rồi", null);
        }

        // Edge case 3: Must be PENDING
        if (invitation.Status != "PENDING")
        {
            return (false, $"Lời mời này đã {invitation.Status.ToLower()}, không thể chấp nhận", null);
        }

        // Edge case 4: Both members must not be deleted
        if (invitation.SenderMember.IsDeleted == true)
        {
            return (false, $"{invitation.SenderMember.FullName} không còn tồn tại trong hệ thống", null);
        }

        if (invitation.ReceiverMember.IsDeleted == true)
        {
            return (false, "Tài khoản của bạn không còn tồn tại trong hệ thống", null);
        }

        // Edge case 5: Both must still be SINGLE
        if (invitation.SenderMember.RelationshipStatus != "SINGLE")
        {
            return (false, $"{invitation.SenderMember.FullName} không còn SINGLE nữa", null);
        }

        if (invitation.ReceiverMember.RelationshipStatus != "SINGLE")
        {
            return (false, "Bạn không còn SINGLE nữa", null);
        }

        // Edge case 6: Both must not have active couple
        var senderHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(invitation.SenderMemberId);
        if (senderHasCouple != null)
        {
            return (false, $"{invitation.SenderMember.FullName} đã có cặp đôi rồi", null);
        }

        var receiverHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(invitation.ReceiverMemberId);
        if (receiverHasCouple != null)
        {
            return (false, "Bạn đã có cặp đôi rồi", null);
        }

        // Update invitation status and timestamps
        invitation.Status = "ACCEPTED";
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.CoupleInvitations.UpdateAsync(invitation);

        // IMPORTANT: Save invitation status first before cancelling other invitations
        await _unitOfWork.SaveChangesAsync();

        // Check if couple profile already exists (even if SEPARATED/INACTIVE)
        // Need to check both (sender, receiver) and (receiver, sender) due to unique constraint
        var existingCouple = await _unitOfWork.Context.Set<CoupleProfile>()
            .FirstOrDefaultAsync(c => 
                (c.MemberId1 == invitation.SenderMemberId && c.MemberId2 == invitation.ReceiverMemberId) ||
                (c.MemberId1 == invitation.ReceiverMemberId && c.MemberId2 == invitation.SenderMemberId));

        CoupleProfile coupleProfile;
        
        if (existingCouple != null)
        {
            // Reactivate existing couple profile
            existingCouple.Status = "ACTIVE";
            existingCouple.StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
            existingCouple.UpdatedAt = DateTime.UtcNow;
            existingCouple.IsDeleted = false;
            
            _unitOfWork.Context.Set<CoupleProfile>().Update(existingCouple);
            coupleProfile = existingCouple;
        }
        else
        {
            // Create new couple profile
            coupleProfile = new CoupleProfile
            {
                MemberId1 = invitation.SenderMemberId,
                MemberId2 = invitation.ReceiverMemberId,
                Status = "ACTIVE",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.CoupleProfiles.AddAsync(coupleProfile);
        }

        // Update relationship status for both members
        invitation.SenderMember.RelationshipStatus = "IN_RELATIONSHIP";
        _unitOfWork.MembersProfile.Update(invitation.SenderMember);

        invitation.ReceiverMember.RelationshipStatus = "IN_RELATIONSHIP";
        _unitOfWork.MembersProfile.Update(invitation.ReceiverMember);

        // Cancel all other pending invitations for both members
        // (Now the current invitation is already ACCEPTED, so it won't be cancelled)
        await _unitOfWork.CoupleInvitations.CancelAllPendingInvitationsForMemberAsync(invitation.SenderMemberId);
        await _unitOfWork.CoupleInvitations.CancelAllPendingInvitationsForMemberAsync(invitation.ReceiverMemberId);

        await _unitOfWork.SaveChangesAsync();

        // TODO: Send push notifications to both

        var response = new AcceptInvitationResponse
        {
            InvitationId = invitation.Id,
            Status = invitation.Status,
            RespondedAt = invitation.RespondedAt.Value,
            CoupleProfile = new CoupleProfileInfo
            {
                CoupleId = coupleProfile.id,
                MemberId1 = coupleProfile.MemberId1,
                Member1Name = invitation.SenderMember.FullName ?? "Unknown",
                MemberId2 = coupleProfile.MemberId2,
                Member2Name = invitation.ReceiverMember.FullName ?? "Unknown",
                Status = coupleProfile.Status ?? "ACTIVE",
                CreatedAt = coupleProfile.CreatedAt ?? DateTime.UtcNow
            }
        };

        return (true, "Đã chấp nhận lời mời ghép đôi thành công! 💕", response);
    }

    public async Task<(bool Success, string Message)> RejectInvitationAsync(int invitationId, int currentMemberId)
    {
        var invitation = await _unitOfWork.CoupleInvitations.GetByIdAsync(invitationId);
        if (invitation == null || invitation.IsDeleted == true)
        {
            return (false, "Không tìm thấy lời mời này");
        }

        // Edge case 1: Only receiver can reject
        if (invitation.ReceiverMemberId != currentMemberId)
        {
            return (false, "Bạn không có quyền từ chối lời mời này");
        }

        // Edge case 2: Cannot reject if already accepted
        if (invitation.Status == "ACCEPTED")
        {
            return (false, "Không thể từ chối lời mời đã được chấp nhận");
        }

        // Edge case 3: Cannot reject if couple already created
        var receiverHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(currentMemberId);
        if (receiverHasCouple != null)
        {
            return (false, "Bạn đã có cặp đôi, không thể từ chối lời mời này");
        }

        // Edge case 4: Must be PENDING
        if (invitation.Status != "PENDING")
        {
            return (false, $"Lời mời này đã được {invitation.Status.ToLower()}");
        }

        // Update invitation status and timestamps
        invitation.Status = "REJECTED";
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CoupleInvitations.UpdateAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

        // TODO: Send push notification to sender

        return (true, "Đã từ chối lời mời ghép đôi");
    }

    public async Task<(bool Success, string Message)> CancelInvitationAsync(int invitationId, int currentMemberId)
    {
        var invitation = await _unitOfWork.CoupleInvitations.GetByIdAsync(invitationId);
        if (invitation == null || invitation.IsDeleted == true)
        {
            return (false, "Không tìm thấy lời mời này");
        }

        // Edge case 1: Only sender can cancel
        if (invitation.SenderMemberId != currentMemberId)
        {
            return (false, "Bạn không có quyền hủy lời mời này");
        }

        // Edge case 2: Cannot cancel if already accepted
        if (invitation.Status == "ACCEPTED")
        {
            return (false, "Không thể hủy lời mời đã được chấp nhận");
        }

        // Edge case 3: Cannot cancel if couple already created
        var senderHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(currentMemberId);
        if (senderHasCouple != null)
        {
            return (false, "Bạn đã có cặp đôi, không thể hủy lời mời này");
        }

        // Edge case 4: Must be PENDING
        if (invitation.Status != "PENDING")
        {
            return (false, $"Lời mời này đã được {invitation.Status.ToLower()}");
        }

        // Update status and timestamps
        invitation.Status = "CANCELLED";
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CoupleInvitations.UpdateAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Đã hủy lời mời ghép đôi");
    }

    public async Task<(bool Success, string Message)> BreakupAsync(int currentMemberId)
    {
        // Edge case 1: Member must have an active couple
        var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(currentMemberId);
        if (couple == null)
        {
            return (false, "Bạn chưa có cặp đôi để chia tay");
        }

        // Edge case 2: Couple must be ACTIVE
        if (couple.Status != "ACTIVE")
        {
            return (false, $"Cặp đôi đã {couple.Status?.ToLower()}, không thể chia tay");
        }

        // Get both members
        var member1 = await _unitOfWork.MembersProfile.GetByIdAsync(couple.MemberId1);
        var member2 = await _unitOfWork.MembersProfile.GetByIdAsync(couple.MemberId2);

        // Edge case 3: Both members must exist and not be deleted
        if (member1 == null || member1.IsDeleted == true || member2 == null || member2.IsDeleted == true)
        {
            return (false, "Không tìm thấy thông tin thành viên trong cặp đôi");
        }

        // Update couple profile status to SEPARATED
        couple.Status = "SEPARATED";
        couple.UpdatedAt = DateTime.UtcNow;
        couple.IsDeleted = false; // Keep record for history

        _unitOfWork.CoupleProfiles.Update(couple);

        // Update both members' relationship status back to SINGLE
        member1.RelationshipStatus = "SINGLE";
        member1.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.MembersProfile.Update(member1);

        member2.RelationshipStatus = "SINGLE";
        member2.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.MembersProfile.Update(member2);

        // Save all changes
        await _unitOfWork.SaveChangesAsync();

        // Determine who initiated the breakup for messaging
        var partnerName = currentMemberId == couple.MemberId1 ? member2.FullName : member1.FullName;

        // TODO: Send push notification to partner about breakup

        return (true, $"Đã chia tay với {partnerName}. Chúc bạn sớm tìm được người phù hợp hơn!");
    }

    public async Task<List<CoupleInvitationResponse>> GetReceivedInvitationsAsync(
        int memberId,
        string? status,
        int page,
        int pageSize)
    {
        var invitations = await _unitOfWork.CoupleInvitations.GetReceivedInvitationsAsync(memberId, status, page, pageSize);

        return invitations.Select(i => new CoupleInvitationResponse
        {
            InvitationId = i.Id,
            SenderMemberId = i.SenderMemberId,
            SenderName = i.SenderMember?.FullName ?? "Unknown",
            SenderAvatarUrl = i.SenderMember?.User?.AvatarUrl,
            ReceiverMemberId = i.ReceiverMemberId,
            ReceiverName = "", // Not needed for received invitations
            ReceiverAvatarUrl = null,
            InviteCodeUsed = i.InviteCodeUsed,
            Status = i.Status,
            Message = i.Message,
            SentAt = i.SentAt,
            RespondedAt = i.RespondedAt
        }).ToList();
    }

    public async Task<List<CoupleInvitationResponse>> GetSentInvitationsAsync(
        int memberId,
        string? status,
        int page,
        int pageSize)
    {
        var invitations = await _unitOfWork.CoupleInvitations.GetSentInvitationsAsync(memberId, status, page, pageSize);

        return invitations.Select(i => new CoupleInvitationResponse
        {
            InvitationId = i.Id,
            SenderMemberId = i.SenderMemberId,
            SenderName = "", // Not needed for sent invitations
            SenderAvatarUrl = null,
            ReceiverMemberId = i.ReceiverMemberId,
            ReceiverName = i.ReceiverMember?.FullName ?? "Unknown",
            ReceiverAvatarUrl = i.ReceiverMember?.User?.AvatarUrl,
            InviteCodeUsed = i.InviteCodeUsed,
            Status = i.Status,
            Message = i.Message,
            SentAt = i.SentAt,
            RespondedAt = i.RespondedAt
        }).ToList();
    }

    public async Task<List<MemberProfileResponse>> SearchMembersAsync(
        string query,
        int currentMemberId,
        int page,
        int pageSize)
    {
        var currentMember = await _unitOfWork.Context.MemberProfiles
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == currentMemberId);
            
        if (currentMember == null)
            return new List<MemberProfileResponse>();

        var normalizedQuery = string.IsNullOrWhiteSpace(query) 
            ? null 
            : Helpers.VietnameseTextHelper.NormalizeForSearch(query);

        IQueryable<MemberProfile> baseQuery = _unitOfWork.Context.MemberProfiles
            .Include(m => m.User)
            .Where(m => m.IsDeleted != true && m.Id != currentMemberId && m.FullName != null &&
                       m.User != null && m.User.IsDeleted != true && m.User.Role == "MEMBER");

        List<MemberProfile> candidates;

        // Khi query = null, áp dụng matching algorithm
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var targetGender = currentMember.Gender == "MALE" ? "FEMALE" : 
                              currentMember.Gender == "FEMALE" ? "MALE" : null;

            // Lấy personality của current member (1 query riêng nhỏ)
            var currentPersonality = await _unitOfWork.Context.PersonalityTests
                .Where(pt => pt.MemberId == currentMemberId && pt.IsDeleted != true && 
                           pt.Status == "COMPLETED" && pt.ResultCode != null)
                .OrderByDescending(pt => pt.TakenAt)
                .Select(pt => pt.ResultCode)
                .FirstOrDefaultAsync();

            // Filter chỉ gender (bắt buộc) và area (bắt buộc phải bằng nhau)
            var matchingMembers = await baseQuery
                .Where(m => m.Gender == targetGender && m.area == currentMember.area)
                .Include(m => m.PersonalityTests.Where(pt => pt.IsDeleted != true && pt.Status == "COMPLETED"))
                .ToListAsync();

            // Sắp xếp theo độ ưu tiên (không loại bỏ, chỉ ưu tiên): Mood > Personality > Interests > Name
            candidates = matchingMembers
                .OrderByDescending(m => m.MoodTypesId == currentMember.MoodTypesId)  // Ưu tiên 1: Cùng mood
                .ThenByDescending(m => 
                {
                    // Ưu tiên 2: Cùng personality
                    if (string.IsNullOrWhiteSpace(currentPersonality)) return false;
                    var memberPersonality = m.PersonalityTests.OrderByDescending(pt => pt.TakenAt).FirstOrDefault()?.ResultCode;
                    return !string.IsNullOrWhiteSpace(memberPersonality) && memberPersonality == currentPersonality;
                })
                .ThenByDescending(m => 
                {
                    // Ưu tiên 3: Có overlap interests
                    if (string.IsNullOrWhiteSpace(currentMember.Interests) || string.IsNullOrWhiteSpace(m.Interests)) return false;
                    return m.Interests.Contains(currentMember.Interests) || currentMember.Interests.Contains(m.Interests);
                })
                .ThenBy(m => m.FullName)  // Sắp xếp theo tên
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        }
        else
        {
            // Query có search text - filter ngay trong SQL
            // TODO: Cần tạo computed column hoặc function để search Vietnamese normalized
            candidates = await baseQuery
                .OrderBy(m => m.FullName)
                .ToListAsync(); // Load tất cả để filter normalized name trong memory
            
            candidates = candidates
                .Where(m => Helpers.VietnameseTextHelper.NormalizeForSearch(m.FullName ?? "").Contains(normalizedQuery))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        if (!candidates.Any())
            return new List<MemberProfileResponse>();

        return await BuildMemberResponsesAsync(currentMemberId, currentMember, candidates);
    }

    private async Task<List<MemberProfileResponse>> BuildMemberResponsesAsync(
        int currentMemberId,
        MemberProfile currentMember,
        List<MemberProfile> pagedMembers)
    {
        // Batch queries - get all data at once instead of N queries in loop
        var currentHasCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(currentMemberId);
        
        var memberIds = pagedMembers.Select(m => m.Id).ToList();
        var membersWithCouples = await _unitOfWork.Context.CoupleProfiles
            .Where(c => c.Status == "ACTIVE" && 
                       (memberIds.Contains(c.MemberId1) || memberIds.Contains(c.MemberId2)))
            .Select(c => new { c.MemberId1, c.MemberId2 })
            .ToListAsync();
        
        var memberIdsWithCouples = membersWithCouples
            .SelectMany(c => new[] { c.MemberId1, c.MemberId2 })
            .Distinct()
            .ToHashSet();

        var pendingInvitations = await _unitOfWork.Context.CoupleInvitations
            .Where(i => i.Status == "PENDING" && i.IsDeleted == false &&
                       ((i.SenderMemberId == currentMemberId && memberIds.Contains(i.ReceiverMemberId)) ||
                        (i.ReceiverMemberId == currentMemberId && memberIds.Contains(i.SenderMemberId))))
            .Select(i => new { i.SenderMemberId, i.ReceiverMemberId })
            .ToListAsync();
        
        var pendingMemberIds = pendingInvitations
            .SelectMany(i => new[] { i.SenderMemberId, i.ReceiverMemberId })
            .Where(id => id != currentMemberId)
            .Distinct()
            .ToHashSet();

        // Build response
        return pagedMembers.Select(member =>
        {
            // Debug: Check each condition
            var isCurrentSingle = currentMember?.RelationshipStatus == "SINGLE";
            var currentNoCouple = currentHasCouple == null;
            var isMemberSingle = member.RelationshipStatus == "SINGLE";
            var memberNoCouple = !memberIdsWithCouples.Contains(member.Id);
            var noPendingInvitation = !pendingMemberIds.Contains(member.Id);
            
            var canSend = isCurrentSingle && currentNoCouple && isMemberSingle && memberNoCouple && noPendingInvitation;

            // TODO: Remove debug logging after testing
            if (!canSend)
            {
                Console.WriteLine($"[DEBUG] CanSend=false for Member {member.Id} ({member.FullName}):");
                Console.WriteLine($"  - Current SINGLE: {isCurrentSingle}");
                Console.WriteLine($"  - Current No Couple: {currentNoCouple}");
                Console.WriteLine($"  - Member SINGLE: {isMemberSingle}");
                Console.WriteLine($"  - Member No Couple: {memberNoCouple}");
                Console.WriteLine($"  - No Pending: {noPendingInvitation}");
            }

            return new MemberProfileResponse
            {
                MemberProfileId = member.Id,
                UserId = member.UserId,
                FullName = member.FullName ?? "Unknown",
                AvatarUrl = member.User?.AvatarUrl,
                DateOfBirth = member.DateOfBirth,
                Gender = member.Gender,
                Bio = member.Bio,
                RelationshipStatus = member.RelationshipStatus ?? "SINGLE",
                HomeLatitude = member.HomeLatitude,
                HomeLongitude = member.HomeLongitude,
                BudgetMin = member.BudgetMin,
                BudgetMax = member.BudgetMax,
                Interests = member.Interests,
                AvailableTime = member.AvailableTime,
                Address = member.address,
                Area = member.area,
                InviteCode = member.InviteCode,
                CanSendInvitation = canSend
            };
        }).ToList();
    }
}


