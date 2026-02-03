using capstone_backend.Business.DTOs.Member;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MemberService> _logger;

    public MemberService(IUnitOfWork unitOfWork, ILogger<MemberService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CoupleProfileResponse> InviteMemberAsync(
        int currentUserId,
        string inviteCode)
    {
        // 1. Lấy member profile của người gọi API (người nữ - người nhập invite code)
        var currentMemberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(currentUserId);
        if (currentMemberProfile == null)
            throw new InvalidOperationException("Current user does not have a member profile");

        // 2. Tìm member profile theo invite code được nhập vào (người nam - người được mời)
        var partnerMemberProfile = await _unitOfWork.MembersProfile.GetByInviteCodeAsync(inviteCode);
        if (partnerMemberProfile == null)
            throw new InvalidOperationException($"No member found with invite code '{inviteCode}'");

        // 3. Kiểm tra không thể invite chính mình
        if (currentMemberProfile.Id == partnerMemberProfile.Id)
            throw new InvalidOperationException("Cannot invite yourself");

        // 4. Kiểm tra xem 2 member đã có couple profile chưa
        var existingCouple = await _unitOfWork.Context.Set<CoupleProfile>()
            .Where(c => c.IsDeleted != true &&
                       ((c.MemberId1 == currentMemberProfile.Id && c.MemberId2 == partnerMemberProfile.Id) ||
                        (c.MemberId1 == partnerMemberProfile.Id && c.MemberId2 == currentMemberProfile.Id)))
            .FirstOrDefaultAsync();

        if (existingCouple != null)
            throw new InvalidOperationException("Couple profile already exists for these members");

        // 5. Tạo couple profile mới
        // Đảm bảo member_id_1 < member_id_2 để thỏa constraint ck_member_order
        var smallerId = Math.Min(partnerMemberProfile.Id, currentMemberProfile.Id);
        var largerId = Math.Max(partnerMemberProfile.Id, currentMemberProfile.Id);
        
        var coupleProfile = new CoupleProfile
        {
            MemberId1 = smallerId,
            MemberId2 = largerId,
            CoupleName = $"{partnerMemberProfile.FullName} ❤️ {currentMemberProfile.FullName}",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "ACTIVE",
            TotalPoints = 0,
            InteractionPoints = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<CoupleProfile>().AddAsync(coupleProfile);
        
        // 6. Cập nhật relationship_status của cả 2 member về IN_RELATIONSHIP
        partnerMemberProfile.RelationshipStatus = "IN_RELATIONSHIP";
        partnerMemberProfile.UpdatedAt = DateTime.UtcNow;
        
        currentMemberProfile.RelationshipStatus = "IN_RELATIONSHIP";
        currentMemberProfile.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Created couple profile {CoupleId} for partner member {PartnerId} (invite code owner) and current member {CurrentId} (invite code sender). Updated both members' relationship status to IN_RELATIONSHIP",
            coupleProfile.id, partnerMemberProfile.Id, currentMemberProfile.Id);

        // 7. Trả về response
        var member1 = smallerId == partnerMemberProfile.Id ? partnerMemberProfile : currentMemberProfile;
        var member2 = largerId == partnerMemberProfile.Id ? partnerMemberProfile : currentMemberProfile;
        
        return new CoupleProfileResponse
        {
            Id = coupleProfile.id,
            MemberId1 = coupleProfile.MemberId1,
            MemberId2 = coupleProfile.MemberId2,
            CoupleName = coupleProfile.CoupleName,
            StartDate = coupleProfile.StartDate,
            AniversaryDate = coupleProfile.AniversaryDate,
            Status = coupleProfile.Status,
            CreatedAt = coupleProfile.CreatedAt ?? DateTime.UtcNow,
            Member1Name = member1.FullName,
            Member2Name = member2.FullName
        };
    }
}
