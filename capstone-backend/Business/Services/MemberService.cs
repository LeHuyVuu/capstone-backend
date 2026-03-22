using capstone_backend.Business.DTOs.CoupleInvitation;
using capstone_backend.Business.DTOs.Member;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class MemberService : IMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MemberService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILeaderboardService _leaderboardService;

    public MemberService(IUnitOfWork unitOfWork, ILogger<MemberService> logger, IConfiguration configuration, ILeaderboardService leaderboardService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _leaderboardService = leaderboardService;
    }

    public async Task<CoupleProfileResponse> InviteMemberAsync(
        int currentUserId,
        string inviteCode)
    {
        // 0. Validate invite code
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ArgumentException("Invite code cannot be empty");

        // Sử dụng transaction để tránh race condition
        using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Lấy member profile của người gọi API (người nữ - người nhập invite code)
            var currentMemberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(currentUserId);
            if (currentMemberProfile == null)
                throw new InvalidOperationException("Current user does not have a member profile");
            
            if (currentMemberProfile.IsDeleted == true)
                throw new InvalidOperationException("Current user profile is deleted");

            // 2. Tìm member profile theo invite code được nhập vào (người nam - người được mời)
            var partnerMemberProfile = await _unitOfWork.MembersProfile.GetByInviteCodeAsync(inviteCode);
            if (partnerMemberProfile == null)
                throw new InvalidOperationException($"No member found with invite code '{inviteCode}'");
            
            if (partnerMemberProfile.IsDeleted == true)
                throw new InvalidOperationException("The member you are trying to invite is deleted");

        // 3. Kiểm tra không thể invite chính mình
        if (currentMemberProfile.Id == partnerMemberProfile.Id)
            throw new InvalidOperationException("Cannot invite yourself");

        // 3.1. Kiểm tra gender - chỉ cho phép nam + nữ
        if (string.IsNullOrWhiteSpace(currentMemberProfile.Gender) || 
            string.IsNullOrWhiteSpace(partnerMemberProfile.Gender))
            throw new InvalidOperationException("Both members must have gender specified");
            
        if (currentMemberProfile.Gender == partnerMemberProfile.Gender)
            throw new InvalidOperationException("Can only create couple with different genders");

        if (currentMemberProfile.RelationshipStatus == RelationshipStatus.IN_RELATIONSHIP.ToString())
            throw new InvalidOperationException("You are already marked as in a relationship");
            
        if (partnerMemberProfile.RelationshipStatus == RelationshipStatus.IN_RELATIONSHIP.ToString())
            throw new InvalidOperationException("The member you are trying to invite is already marked as in a relationship");

        var currentMemberInCouple = await _unitOfWork.Context.CoupleProfiles
            .Where(c => c.IsDeleted != true &&
                       c.Status == CoupleProfileStatus.ACTIVE.ToString() &&
                       (c.MemberId1 == currentMemberProfile.Id || c.MemberId2 == currentMemberProfile.Id))
            .FirstOrDefaultAsync();

        if (currentMemberInCouple != null)
            throw new InvalidOperationException("You are already in an active couple. Cannot invite another member.");

        var partnerInCouple = await _unitOfWork.Context.CoupleProfiles
            .Where(c => c.IsDeleted != true &&
                       c.Status == CoupleProfileStatus.ACTIVE.ToString() &&
                       (c.MemberId1 == partnerMemberProfile.Id || c.MemberId2 == partnerMemberProfile.Id))
            .FirstOrDefaultAsync();

        if (partnerInCouple != null)
            throw new InvalidOperationException("The member you are trying to invite is already in an active couple.");

        // 5. Tạo couple profile mới
        // Đảm bảo member_id_1 < member_id_2 để thỏa constraint ck_member_order
        var smallerId = Math.Min(partnerMemberProfile.Id, currentMemberProfile.Id);
        var largerId = Math.Max(partnerMemberProfile.Id, currentMemberProfile.Id);
        
        // Tạo couple name với giới hạn độ dài
        var coupleName = $"{partnerMemberProfile.FullName} ❤️ {currentMemberProfile.FullName}";
        if (coupleName.Length > 200)
            coupleName = coupleName.Substring(0, 197) + "...";
        
        var coupleProfile = new CoupleProfile
        {
            MemberId1 = smallerId,
            MemberId2 = largerId,
            CoupleName = coupleName,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = CoupleProfileStatus.ACTIVE.ToString(),
            TotalPoints = 0,
            InteractionPoints = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<CoupleProfile>().AddAsync(coupleProfile);
        
        partnerMemberProfile.RelationshipStatus = RelationshipStatus.IN_RELATIONSHIP.ToString();
        partnerMemberProfile.UpdatedAt = DateTime.UtcNow;
        
        currentMemberProfile.RelationshipStatus = RelationshipStatus.IN_RELATIONSHIP.ToString();
        currentMemberProfile.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        // Thêm couple vào leaderboard ngay
        await _leaderboardService.AddCoupleToLeaderboardAsync(coupleProfile.id);

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
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }


    public async Task<InviteInfoResponse> GetInviteInfoAsync(int currentUserId)
    {
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(currentUserId);
        if (memberProfile == null)
            throw new InvalidOperationException("Member profile not found");

        if (string.IsNullOrEmpty(memberProfile.InviteCode))
            throw new InvalidOperationException("Invite code not generated for this user");

        // Đọc cấu hình từ appsettings.json
        var useRedirectPage = _configuration.GetValue<bool>("DeepLink:UseRedirectPage");
        var baseUrl = _configuration["DeepLink:BaseUrl"] ?? "https://your-domain.com";
        var devScheme = _configuration["DeepLink:DevScheme"] ?? "couplejoy";
        
        string deepLink;

        if (useRedirectPage)
        {
            // Production: Dùng redirect page (tự động mở app hoặc hiển thị trang tải)
            // Link: https://your-domain.com/invite/ABC123
            deepLink = $"{baseUrl}/invite/{memberProfile.InviteCode}";
        }
        else
        {
            // Dev mode: Custom URL Scheme trực tiếp
            deepLink = $"{devScheme}://invite?code={memberProfile.InviteCode}";
        }

        return new InviteInfoResponse
        {
            InviteCode = memberProfile.InviteCode,
            InviteLink = deepLink
        };
    }

    public async Task<MemberProfileResponse> UpdateMemberProfileAsync(int currentUserId, UpdateMemberProfileRequest request)
    {
        var memberProfile = await _unitOfWork.Context.MemberProfiles
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == currentUserId);
            
        if (memberProfile == null)
            throw new InvalidOperationException("Member profile not found");

        if (memberProfile.IsDeleted == true)
            throw new InvalidOperationException("Member profile is deleted");

        // Check if at least one field is provided
        bool hasUpdates = false;

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            memberProfile.FullName = request.FullName;
            hasUpdates = true;
        }

        if (request.DateOfBirth.HasValue)
        {
            memberProfile.DateOfBirth = request.DateOfBirth.Value;
            hasUpdates = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (request.Gender != "MALE" && request.Gender != "FEMALE")
                throw new ArgumentException("Gender must be MALE or FEMALE");
            
            // Check if user is in a couple - cannot change gender if in couple
            var isInCouple = await _unitOfWork.Context.CoupleProfiles
                .AnyAsync(c => c.IsDeleted != true &&
                             c.Status == CoupleProfileStatus.ACTIVE.ToString() &&
                             (c.MemberId1 == memberProfile.Id || c.MemberId2 == memberProfile.Id));
            
            if (isInCouple && memberProfile.Gender != request.Gender)
                throw new InvalidOperationException("Cannot change gender while in a couple");
            
            memberProfile.Gender = request.Gender;
            hasUpdates = true;
        }

        if (request.Bio != null) // Allow empty string to clear bio
        {
            memberProfile.Bio = request.Bio;
            hasUpdates = true;
        }

        if (request.HomeLatitude.HasValue)
        {
            memberProfile.HomeLatitude = request.HomeLatitude.Value;
            hasUpdates = true;
        }

        if (request.HomeLongitude.HasValue)
        {
            memberProfile.HomeLongitude = request.HomeLongitude.Value;
            hasUpdates = true;
        }

        if (request.BudgetMin.HasValue)
        {
            memberProfile.BudgetMin = request.BudgetMin.Value;
            hasUpdates = true;
        }

        if (request.BudgetMax.HasValue)
        {
            memberProfile.BudgetMax = request.BudgetMax.Value;
            hasUpdates = true;
        }

        if (request.Address != null)
        {
            memberProfile.address = request.Address;
            hasUpdates = true;
        }

        if (request.Area != null)
        {
            memberProfile.area = request.Area;
            hasUpdates = true;
        }

        // Update UserAccount fields (avatar and phone)
        if (request.AvatarUrl != null && memberProfile.User != null)
        {
            memberProfile.User.AvatarUrl = request.AvatarUrl;
            memberProfile.User.UpdatedAt = DateTime.UtcNow;
            hasUpdates = true;
        }

        if (request.PhoneNumber != null && memberProfile.User != null)
        {
            // Validate phone number format (Vietnamese phone number)
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                // Remove spaces and special characters
                var cleanPhone = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
                
                // Vietnamese phone: 10 digits starting with 0, or 9 digits without 0
                if (cleanPhone.Length < 9 || cleanPhone.Length > 11)
                    throw new ArgumentException("Phone number must be 9-11 digits");
                
                // Must start with 0 if 10 digits
                if (cleanPhone.Length == 10 && !cleanPhone.StartsWith("0"))
                    throw new ArgumentException("10-digit phone number must start with 0");
                
                // Valid prefixes for Vietnamese mobile: 03, 05, 07, 08, 09
                if (cleanPhone.StartsWith("0"))
                {
                    var prefix = cleanPhone.Substring(0, 2);
                    if (prefix != "03" && prefix != "05" && prefix != "07" && prefix != "08" && prefix != "09")
                        throw new ArgumentException("Invalid Vietnamese phone number prefix");
                }
                
                memberProfile.User.PhoneNumber = cleanPhone;
            }
            else
            {
                // Allow empty string to clear phone number
                memberProfile.User.PhoneNumber = request.PhoneNumber;
            }
            
            memberProfile.User.UpdatedAt = DateTime.UtcNow;
            hasUpdates = true;
        }

        if (!hasUpdates)
            throw new ArgumentException("Chưa có gì được cập nhật");

        memberProfile.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.MemberProfiles.Update(memberProfile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated member profile {MemberId} for user {UserId}", memberProfile.Id, currentUserId);

        return new MemberProfileResponse
        {
            MemberProfileId = memberProfile.Id,
            UserId = memberProfile.UserId,
            FullName = memberProfile.FullName,
            AvatarUrl = memberProfile.User?.AvatarUrl,
            PhoneNumber = memberProfile.User?.PhoneNumber,
            DateOfBirth = memberProfile.DateOfBirth,
            Gender = memberProfile.Gender,
            Bio = memberProfile.Bio,
            RelationshipStatus = memberProfile.RelationshipStatus,
            HomeLatitude = memberProfile.HomeLatitude,
            HomeLongitude = memberProfile.HomeLongitude,
            BudgetMin = memberProfile.BudgetMin,
            BudgetMax = memberProfile.BudgetMax,
            Interests = memberProfile.Interests,
            AvailableTime = memberProfile.AvailableTime,
            Address = memberProfile.address,
            Area = memberProfile.area,
            InviteCode = memberProfile.InviteCode
        };
    }
}
