using System.Text.Json;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.DTOs.MoodType;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class MoodTypeService : IMoodTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoodTypeService> _logger;
    private readonly IMoodMappingService _moodMappingService;
    private readonly IChallengeService _challengeService;
    private readonly IFcmService? _fcmService;
    private readonly IConversationRepository _conversationRepository;

    public MoodTypeService(
        IUnitOfWork unitOfWork, 
        ILogger<MoodTypeService> logger, 
        IMoodMappingService moodMappingService, 
        IChallengeService challengeService,
        IFcmService? fcmService,
        IConversationRepository conversationRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _moodMappingService = moodMappingService;
        _challengeService = challengeService;
        _fcmService = fcmService;
        _conversationRepository = conversationRepository;
    }

    public async Task<List<MoodTypeResponse>> GetAllMoodTypesAsync(string? gender, CancellationToken cancellationToken = default)
    {
        var moodTypes = await _unitOfWork.MoodTypes.GetAllActiveAsync(cancellationToken: cancellationToken);
        return moodTypes.Select(m => MapToResponse(m, gender)).ToList();
    }

    public async Task<MoodTypeResponse?> GetMoodTypeByIdAsync(int id, string? gender, CancellationToken cancellationToken = default)
    {
        var moodType = await _unitOfWork.MoodTypes.GetByIdActiveAsync(id, cancellationToken: cancellationToken);
        return moodType == null ? null : MapToResponse(moodType, gender);
    }

    public async Task<UpdateMoodTypeResponse?> UpdateMoodTypeForUserAsync(int userId, int moodTypeId, CancellationToken cancellationToken = default)
    {
        // Kiểm tra mood type có tồn tại không
        var moodType = await _unitOfWork.MoodTypes.GetByIdActiveAsync(moodTypeId, cancellationToken: cancellationToken);

        if (moodType == null)
        {
            _logger.LogWarning($"Không tìm thấy mood type với ID {moodTypeId}");
            return null;
        }

        // Lấy member profile của user
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId, cancellationToken: cancellationToken);

        if (memberProfile == null)
        {
            _logger.LogWarning($"Không tìm thấy member profile cho user {userId}");
            return null;
        }

        // Cập nhật mood type ID
        memberProfile.MoodTypesId = moodType.Id;
        memberProfile.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.MembersProfile.Update(memberProfile);

        // Lấy URL ảnh dựa vào gender của member
        var gender = (memberProfile.Gender ?? "").Trim().ToLowerInvariant();
        if (gender != "male" && gender != "female") gender = "female"; // default
        var imageUrl = ResolveIconUrl(moodType.IconUrl, gender);

        // Lấy giờ VN (UTC+7)
        var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        var todayVN = nowVN.Date;

        // Kiểm tra xem hôm nay (theo giờ VN) đã có MoodLog chưa
        var existingLog = await _unitOfWork.Context.Set<MemberMoodLog>()
            .Where(m => m.MemberId == memberProfile.Id 
                     && m.CreatedAt.HasValue 
                     && m.IsDeleted != true)
            .ToListAsync(cancellationToken);

        // Filter theo ngày VN (do database lưu UTC)
        var todayLog = existingLog
            .Where(m => {
                var logDateVN = TimeZoneInfo.ConvertTimeFromUtc(m.CreatedAt!.Value, vnTimeZone).Date;
                return logDateVN == todayVN;
            })
            .FirstOrDefault();

        if (todayLog != null)
        {
            // Trong cùng ngày → UPDATE MoodTypeId
            todayLog.MoodTypeId = moodType.Id;
            todayLog.UpdatedAt = DateTime.UtcNow; // Luôn lưu UTC
            _unitOfWork.MemberMoodLogs.Update(todayLog);
            _logger.LogInformation($"🔄 Updated existing mood log for today VN (MoodType: {moodType.Name})");
        }
        else
        {
            // Ngày mới → INSERT record mới
            await _unitOfWork.MemberMoodLogs.AddAsync(new MemberMoodLog
            {
                MemberId = memberProfile.Id,
                MoodTypeId = moodType.Id,
                ImageUrl = imageUrl,
                IsPrivate = false,
                CreatedAt = DateTime.UtcNow, // Luôn lưu UTC
                UpdatedAt = DateTime.UtcNow, // Luôn lưu UTC
                IsDeleted = false
            });
            _logger.LogInformation($"➕ Created new mood log for today VN (MoodType: {moodType.Name})");

            await _challengeService.HandleCheckinChallengeProgressAsync(userId);
        }

        await _unitOfWork.SaveChangesAsync();

        // TODO: handle challenge progress update

        _logger.LogInformation($"✅ User {userId} đã cập nhật mood type thành {moodType.Name} (ID: {moodType.Id})");

        // Kiểm tra couple và tạo CoupleMoodLog
        await UpdateCoupleMoodIfNeeded(memberProfile.Id, moodType.Id, cancellationToken);

        // Trả về response
        return new UpdateMoodTypeResponse
        {
            MoodTypeId = moodType.Id,
            MoodTypeName = moodType.Name,
            // IconUrl = moodType.IconUrl,
            UpdatedAt = memberProfile.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public async Task<CurrentMoodResponse?> GetCurrentMoodAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Lấy member profile của user (include User for AvatarUrl)
        var memberProfile = await _unitOfWork.Context.Set<MemberProfile>()
            .Include(m => m.User)
            .Where(m => m.UserId == userId && m.IsDeleted != true)
            .FirstOrDefaultAsync(cancellationToken);
        if (memberProfile == null)
        {
            _logger.LogWarning($"Không tìm thấy member profile cho user {userId}");
            return null;
        }

        // Lấy mood log gần nhất của member hiện tại
        var currentMoodLog = await _unitOfWork.Context.Set<MemberMoodLog>()
            .Where(m => m.MemberId == memberProfile.Id && m.IsDeleted != true)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        string? currentMood = null;
        int? currentMoodId = null;
        DateTime? moodUpdatedAt = null;

        if (currentMoodLog != null)
        {
            var moodType = await _unitOfWork.MoodTypes.GetByIdAsync(currentMoodLog.MoodTypeId);
            if (moodType != null)
            {
                currentMood = TranslateMoodToVietnamese(moodType.Name);
                currentMoodId = moodType.Id;
                moodUpdatedAt = currentMoodLog.UpdatedAt ?? currentMoodLog.CreatedAt;
            }
        }

        // Kiểm tra couple profile
        int? partnerMemberId = null;
        string? partnerMemberAvatarUrl = null;
        string? partnerMood = null;
        int? partnerMoodId = null;
        DateTime? partnerMoodUpdatedAt = null;
        string? coupleMood = null;
        bool isCouple = false;
        bool hasCoupleMood = false;

        // Lấy couple profile ACTIVE (không lấy couple cũ đã chia tay)
        var coupleProfile = await _unitOfWork.Context.Set<CoupleProfile>()
            .Where(c => (c.MemberId1 == memberProfile.Id || c.MemberId2 == memberProfile.Id)
                     && c.Status == "ACTIVE"
                     && c.IsDeleted != true)
            .FirstOrDefaultAsync(cancellationToken);

        if (coupleProfile != null)
        {
            isCouple = true;
            
            // Xác định partner ID
            partnerMemberId = coupleProfile.MemberId1 == memberProfile.Id 
                ? coupleProfile.MemberId2 
                : coupleProfile.MemberId1;

            // Lấy partner member profile để lấy avatar
            var partnerMemberProfile = await _unitOfWork.Context.Set<MemberProfile>()
                .Include(m => m.User)
                .Where(m => m.Id == partnerMemberId.Value && m.IsDeleted != true)
                .FirstOrDefaultAsync(cancellationToken);
            if (partnerMemberProfile != null)
            {
                partnerMemberAvatarUrl = partnerMemberProfile.User?.AvatarUrl;
            }

            // Lấy mood log gần nhất của partner
            var partnerMoodLog = await _unitOfWork.Context.Set<MemberMoodLog>()
                .Where(m => m.MemberId == partnerMemberId && m.IsDeleted != true)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (partnerMoodLog != null)
            {
                var partnerMoodType = await _unitOfWork.MoodTypes.GetByIdAsync(partnerMoodLog.MoodTypeId);
                if (partnerMoodType != null)
                {
                    partnerMood = TranslateMoodToVietnamese(partnerMoodType.Name);
                    partnerMoodId = partnerMoodType.Id;
                    partnerMoodUpdatedAt = partnerMoodLog.UpdatedAt ?? partnerMoodLog.CreatedAt;
                }
            }

            // Nếu cả 2 đều có mood, tính toán couple mood
            if (currentMoodId.HasValue && partnerMoodId.HasValue)
            {
                coupleMood = await _moodMappingService.GetCoupleMoodTypeAsync(currentMoodId.Value, partnerMoodId.Value);
                hasCoupleMood = !string.IsNullOrEmpty(coupleMood);
            }
        }

        // Tạo description cho couple mood
        var description = _unitOfWork.Context.Set<CoupleMoodType>()
            .Where(cmt => cmt.Name == coupleMood)
            .Select(cmt => cmt.Description)
            .FirstOrDefault();

        return new CurrentMoodResponse
        {
            MemberId = memberProfile.Id,
            MemberName = memberProfile.FullName,
            MemberAvatarUrl = memberProfile.User?.AvatarUrl,
            CurrentMood = currentMood,
            CurrentMoodId = currentMoodId,
            MoodUpdatedAt = moodUpdatedAt,
            PartnerMemberId = partnerMemberId,
            PartnerMemberName = partnerMemberId.HasValue ? await _unitOfWork.Context.Set<MemberProfile>()
                .Where(m => m.Id == partnerMemberId.Value)
                .Select(m => m.FullName)
                .FirstOrDefaultAsync(cancellationToken) : null,
            PartnerAvatarUrl = partnerMemberAvatarUrl,
            PartnerMood = partnerMood,
            PartnerMoodId = partnerMoodId,
            PartnerMoodUpdatedAt = partnerMoodUpdatedAt,
            CoupleMood = coupleMood,
            Description = description,
            IsCouple = isCouple,
            HasCoupleMood = hasCoupleMood
        };
    }

    private async Task UpdateCoupleMoodIfNeeded(int memberId, int moodTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Kiểm tra xem member có couple ACTIVE không (không lấy couple cũ)
            var coupleProfile = await _unitOfWork.Context.Set<CoupleProfile>()
                .Where(c => (c.MemberId1 == memberId || c.MemberId2 == memberId)
                         && c.Status == "ACTIVE"
                         && c.IsDeleted != true)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (coupleProfile == null)
            {
                _logger.LogInformation($"Member {memberId} không có couple active");
                return;
            }

            // Xác định partner ID
            var partnerId = coupleProfile.MemberId1 == memberId 
                ? coupleProfile.MemberId2 
                : coupleProfile.MemberId1;

            // Lấy mood của partner
            var partnerProfile = await _unitOfWork.MembersProfile.GetByIdAsync(partnerId);
            if (partnerProfile?.MoodTypesId == null)
            {
                _logger.LogInformation($"Partner {partnerId} chưa có mood");
                return;
            }

            // Tính couple mood type
            var coupleMoodName = await _moodMappingService.GetCoupleMoodTypeAsync(moodTypeId, partnerProfile.MoodTypesId.Value);
            if (string.IsNullOrEmpty(coupleMoodName))
            {
                _logger.LogWarning($"Không thể tính couple mood cho mood1={moodTypeId}, mood2={partnerProfile.MoodTypesId.Value}");
                return;
            }

            // Tìm CoupleMoodType ID từ tên
            var coupleMoodType = await _unitOfWork.Context.CoupleMoodTypes
                .FirstOrDefaultAsync(cmt => cmt.Name == coupleMoodName && cmt.IsActive == true, cancellationToken);
            
            if (coupleMoodType == null)
            {
                _logger.LogWarning($"Không tìm thấy CoupleMoodType với tên '{coupleMoodName}'");
                return;
            }

            // Update CoupleProfile.CoupleMoodTypeId
            coupleProfile.CoupleMoodTypeId = coupleMoodType.Id;
            coupleProfile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.CoupleProfiles.Update(coupleProfile);

            // Tạo CoupleMoodLog
            var coupleMoodLog = new CoupleMoodLog
            {
                CoupleId = coupleProfile.id,
                CoupleMoodTypeId = coupleMoodType.Id,
                Note = $"Auto-generated from member mood update (MemberId: {memberId})",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.Context.CoupleMoodLogs.AddAsync(coupleMoodLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"💑 Đã cập nhật couple mood '{coupleMoodName}' (ID: {coupleMoodType.Id}) cho CoupleId={coupleProfile.id}");

            // Lấy thông tin member vừa đổi mood để gửi notification
            var currentMember = await _unitOfWork.MembersProfile.GetByIdAsync(memberId);
            var currentMoodType = await _unitOfWork.MoodTypes.GetByIdAsync(moodTypeId);
            
            // Gửi notification cho partner
            await SendCoupleMoodNotificationAsync(
                partnerId, 
                currentMember?.FullName, 
                TranslateMoodToVietnamese(currentMoodType?.Name),
                coupleMoodName, 
                coupleMoodType.Description,
                coupleProfile.id, 
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Lỗi khi cập nhật couple mood cho MemberId={memberId}");
            // Không throw exception để không ảnh hưởng đến flow chính
        }
    }

    private async Task SendCoupleMoodNotificationAsync(
        int partnerMemberId, 
        string? senderName,
        string? senderMood,
        string coupleMoodName, 
        string? coupleMoodDescription,
        int coupleId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_fcmService == null)
            {
                _logger.LogWarning("FCM Service không khả dụng, bỏ qua gửi notification");
                return;
            }

            // Lấy partner profile với UserId (query trực tiếp để đảm bảo có UserId)
            var partnerProfile = await _unitOfWork.Context.Set<MemberProfile>()
                .Where(m => m.Id == partnerMemberId && m.IsDeleted != true)
                .Select(m => new { m.UserId, m.FullName })
                .FirstOrDefaultAsync(cancellationToken);
                
            if (partnerProfile == null)
            {
                _logger.LogWarning($"Không tìm thấy partner profile với MemberId={partnerMemberId}");
                return;
            }

            // Lấy device tokens của partner
            var tokens = await _unitOfWork.DeviceTokens.GetTokensByUserId(partnerProfile.UserId);
            if (tokens == null || !tokens.Any())
            {
                _logger.LogInformation($"Không có device tokens cho user ID {partnerProfile.UserId}");
                return;
            }

            // Lấy couple profile để lấy member IDs
            var coupleProfile = await _unitOfWork.Context.Set<CoupleProfile>()
                .Where(c => c.id == coupleId && c.IsDeleted != true)
                .FirstOrDefaultAsync(cancellationToken);

            if (coupleProfile == null)
            {
                _logger.LogWarning($"Không tìm thấy couple profile với ID={coupleId}");
                return;
            }

            // Lấy userId của cả 2 members
            var memberUserIds = await _unitOfWork.Context.Set<MemberProfile>()
                .Where(m => (m.Id == coupleProfile.MemberId1 || m.Id == coupleProfile.MemberId2) && m.IsDeleted != true)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            if (memberUserIds.Count != 2)
            {
                _logger.LogWarning($"Không tìm thấy đủ member profiles cho couple {coupleId}");
                return;
            }

         
         
            // Tạo notification request với gợi ý hành động
            var actionSuggestion = GetActionSuggestionFromCoupleMood(coupleMoodName, coupleMoodDescription);
            var notificationRequest = new SendNotificationRequest
            {
                Title = $"Mood tụi mình vừa thay đổi 💕",
                Body = $"{senderName} đang {senderMood} nè!\nMood của tụi mình giờ là \"{coupleMoodName}\" đó, {actionSuggestion}"
            };

            await _fcmService.SendMultiNotificationAsync(tokens, notificationRequest);
            _logger.LogInformation($"✅ Đã gửi notification couple mood cho partner (UserId: {partnerProfile.UserId})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Lỗi khi gửi notification cho partner MemberId={partnerMemberId}");
            // Không throw exception để không ảnh hưởng đến flow chính
        }
    }

    private string GetActionSuggestionFromCoupleMood(string coupleMoodName, string? description)
{
    return coupleMoodName switch
    {
        "Vui vẻ" => "cùng nhau đi chơi thôiii 🎉",
        "Yên tĩnh" => "tìm chỗ yên tĩnh ngồi cạnh nhau nha 🌿",
        "Cần an ủi" => "ở bên nhau và quan tâm nhau nhiều hơn nhé 🤗",
        "Cân bằng" => "mọi thứ đang ổn, tận hưởng khoảnh khắc này nha 🌼",
        "Hòa hợp" => "quá hợp vibe rồi, làm gì cùng nhau cũng vui 💞",
        "Khám phá" => "cùng nhau khám phá địa điểm mới thôi 🗺️",
        "Tình cảm" => "dành cho nhau chút ngọt ngào đi nè 💕",
        "An tâm" => "chỉ cần ở cạnh nhau là đủ rồi 💝",
        "Động lực" => "cùng nhau cố gắng và phát triển nha 💪",
        "Trung lập" => "rủ nhau làm gì đó nhẹ nhàng thôi 💭",
        "Thư giãn" => "cùng nghỉ ngơi và thư giãn nha 🌙",
        "Hòa giải" => "nhẹ nhàng nói chuyện và hiểu nhau hơn nha 💗",
        _ => "ở cạnh nhau là được rồi đó 💬"
    };
}

    /// <summary>
    /// Translate mood name to Vietnamese using the reusable translation function
    /// If mood is an emotion code (HAPPY, SAD, etc.), it translates to Vietnamese
    /// Otherwise, returns the mood name as-is
    /// </summary>
    private string? TranslateMoodToVietnamese(string? moodName)
    {
        if (string.IsNullOrEmpty(moodName))
            return moodName;

        // Try to translate using emotion mapping (for emotion codes like HAPPY, SAD)
        return FaceEmotionService.MapEmotionToVietnamese(moodName);
    }

    /// <summary>
    /// Get description for couple mood type
    /// </summary>
    private string? GetCoupleMoodDescription(string? coupleMood)
    {
        if (string.IsNullOrEmpty(coupleMood))
            return null;

        return coupleMood switch
        {
            "Vui chung" => "Cả hai đều vui vẻ, năng lượng tích cực. Đây là lúc tốt để cùng nhau tận hưởng những hoạt động vui vẻ, gắn kết, chia sẻ niềm vui.",
            
            "Cả hai yên tĩnh" => "Không gian yên bình, nhẹ nhàng. Phù hợp cho những hoạt động thư giãn, không quá kích thích. Tránh những trải nghiệm quá mạnh mẽ.",
            
            "Cần được an ủi" => "Một người cảm thấy buồn, cần sự hỗ trợ và chia sẻ. Tạo không gian riêng tư, ấm áp, để cùng nói chuyện và thấu hiểu nhau.",
            
            "Căng thẳng hai chiều" => "Cả hai hoặc một trong hai cảm thấy stress, sợ hãi, tức giận. Cần không gian thoáng, an toàn, tránh tiếp xúc mạnh. Tìm cách giảm căng thẳng.",
            
            "Lệch pha cảm xúc" => "Một người vui vẻ trong khi người kia đang buồn/tức giận/sợ hãi. Cần một nơi trung hòa để giúp cân bằng cảm xúc, hiểu biết sâu sắc hơn.",
            
            "Hứng thú khám phá" => "Có sự hứng thú khám phá nhưng an toàn. Đây là lúc tốt để thử những điều mới, khám phá địa điểm mới, trải nghiệm lạ nhưng không quá mạo hiểm.",
            
            "Vui nhưng dễ tổn thương" => "Một người vui vẻ nhưng người kia có chút buồn/sợ hãi. Nên tìm những hoạt động vui nhẹ, tránh đùa quá mạnh hoặc kích thích.",
            
            "Cần được trấn an" => "Một người cảm thấy sợ hãi/lo lắng, cần sự trấn an từ người bình tĩnh/hứng thú. Tạo không gian an toàn, ấm áp, không đông đúc.",
            
            "Giảm thân mật" => "Một người cảm thấy ghê tởm/khó chịu. Cần tránh không gian kín, tiếp xúc gần gũi. Hãy tôn trọng ranh giới và tạo không gian thoáng.",
            
            "Cần hòa giải" => "Có xung đột cảm xúc (tức giận + buồn, hoặc buồn + ghê tởm). Tìm không gian trung lập để nói chuyện, hiểu nhau, giải quyết bất đồng.",
            
            "Năng lượng không đồng đều" => "Một người năng lượng cao trong khi người kia stress. Nên tìm không gian rộng, thoáng, tránh những kích thích thêm. Cần cân bằng.",
            
            "Trung tính" => "Cả hai cảm thấy bình thường, không quá cao không quá thấp. Đây là lúc tốt cho những hoạt động nhẹ nhàng, trung tính, không quá nhiều kích thích.",
            
            _ => coupleMood
        };
    }

    private MoodTypeResponse MapToResponse(MoodType moodType, string? gender)
    {
        return new MoodTypeResponse
        {
            Id = moodType.Id,
            Name = moodType.Name,
            IconUrl = ResolveIconUrl(moodType.IconUrl, gender),
            IsActive = moodType.IsActive,
            CreatedAt = moodType.CreatedAt
        };
    }

    /// <summary>
    /// icon_url trong DB có thể là:
    /// 1) URL thường (legacy): "https://....png" => trả thẳng
    /// 2) JSON string: {"male":"...","female":"..."} => chọn theo gender
    /// </summary>
    private static string? ResolveIconUrl(string? iconUrl, string? gender)
    {
        if (string.IsNullOrWhiteSpace(iconUrl))
            return iconUrl;

        // normalize gender
        var g = (gender ?? "").Trim().ToLowerInvariant();
        if (g != "male" && g != "female")
            g = "female"; // default

        // Nếu là URL cũ (không phải JSON) -> trả luôn
        var trimmed = iconUrl.TrimStart();
        if (!trimmed.StartsWith("{"))
            return iconUrl;

        // Parse JSON {"male":"...","female":"..."}
        try
        {
            using var doc = JsonDocument.Parse(iconUrl);

            // ưu tiên đúng gender
            if (doc.RootElement.TryGetProperty(g, out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                return urlProp.GetString();

            // fallback
            if (doc.RootElement.TryGetProperty("female", out var f) && f.ValueKind == JsonValueKind.String)
                return f.GetString();

            if (doc.RootElement.TryGetProperty("male", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString();

            // JSON không đúng format -> trả nguyên
            return iconUrl;
        }
        catch
        {
            // JSON lỗi -> trả nguyên để không crash
            return iconUrl;
        }
    }
}
