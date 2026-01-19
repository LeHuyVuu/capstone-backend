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
        string inviteCode,
        CancellationToken cancellationToken = default)
    {
        // 1. Lấy member profile của người gọi API (người nữ - người nhập invite code)
        var currentMemberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(currentUserId, cancellationToken: cancellationToken);
        if (currentMemberProfile == null)
            throw new InvalidOperationException("Current user does not have a member profile");

        // 2. Tìm member profile theo invite code được nhập vào (người nam - người được mời)
        var partnerMemberProfile = await _unitOfWork.MembersProfile.GetByInviteCodeAsync(inviteCode, cancellationToken: cancellationToken);
        if (partnerMemberProfile == null)
            throw new InvalidOperationException($"No member found with invite code '{inviteCode}'");

        // 3. Kiểm tra không thể invite chính mình
        if (currentMemberProfile.id == partnerMemberProfile.id)
            throw new InvalidOperationException("Cannot invite yourself");

        // 4. Kiểm tra xem 2 member đã có couple profile chưa
        var existingCouple = await _unitOfWork.Context.Set<couple_profile>()
            .Where(c => c.is_deleted != true &&
                       ((c.member_id_1 == currentMemberProfile.id && c.member_id_2 == partnerMemberProfile.id) ||
                        (c.member_id_1 == partnerMemberProfile.id && c.member_id_2 == currentMemberProfile.id)))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCouple != null)
            throw new InvalidOperationException("Couple profile already exists for these members");

        // 5. Tạo couple profile mới
        // member_id_1 = người được mời (người nam có invite code)
        // member_id_2 = người gọi API (người nữ nhập invite code)
        var coupleProfile = new couple_profile
        {
            member_id_1 = partnerMemberProfile.id,
            member_id_2 = currentMemberProfile.id,
            couple_name = $"{partnerMemberProfile.full_name} ❤️ {currentMemberProfile.full_name}",
            start_date = DateOnly.FromDateTime(DateTime.UtcNow),
            status = "ACTIVE",
            total_points = 0,
            interaction_points = 0,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
            is_deleted = false
        };

        await _unitOfWork.Context.Set<couple_profile>().AddAsync(coupleProfile, cancellationToken);
        
        // 6. Cập nhật relationship_status của cả 2 member về IN_RELATIONSHIP
        partnerMemberProfile.relationship_status = "IN_RELATIONSHIP";
        partnerMemberProfile.updated_at = DateTime.UtcNow;
        
        currentMemberProfile.relationship_status = "IN_RELATIONSHIP";
        currentMemberProfile.updated_at = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created couple profile {CoupleId} for partner member {PartnerId} (invite code owner) and current member {CurrentId} (invite code sender). Updated both members' relationship status to IN_RELATIONSHIP",
            coupleProfile.id, partnerMemberProfile.id, currentMemberProfile.id);

        // 7. Trả về response
        return new CoupleProfileResponse
        {
            Id = coupleProfile.id,
            MemberId1 = coupleProfile.member_id_1,
            MemberId2 = coupleProfile.member_id_2,
            CoupleName = coupleProfile.couple_name,
            StartDate = coupleProfile.start_date,
            AniversaryDate = coupleProfile.aniversary_date,
            Status = coupleProfile.status,
            CreatedAt = coupleProfile.created_at ?? DateTime.UtcNow,
            Member1Name = partnerMemberProfile.full_name,
            Member2Name = currentMemberProfile.full_name
        };
    }
}
