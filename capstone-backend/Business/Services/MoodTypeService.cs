using System.Text.Json;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.DTOs.MoodType;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class MoodTypeService : IMoodTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoodTypeService> _logger;

    public MoodTypeService(IUnitOfWork unitOfWork, ILogger<MoodTypeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<MoodTypeResponse>> GetAllMoodTypesAsync(string? gender, CancellationToken cancellationToken = default)
    {
        var moodTypes = await _unitOfWork.Context.Set<MoodType>()
            .Where(m => m.IsDeleted != true && m.IsActive == true)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return moodTypes.Select(m => MapToResponse(m, gender)).ToList();
    }

    public async Task<MoodTypeResponse?> GetMoodTypeByIdAsync(int id, string? gender, CancellationToken cancellationToken = default)
    {
        var moodType = await _unitOfWork.Context.Set<MoodType>()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted != true, cancellationToken);

        return moodType == null ? null : MapToResponse(moodType, gender);
    }

    public async Task<UpdateMoodTypeResponse?> UpdateMoodTypeForUserAsync(int userId, int moodTypeId, CancellationToken cancellationToken = default)
    {
        // Kiểm tra mood type có tồn tại không
        var moodType = await _unitOfWork.Context.MoodTypes
            .FirstOrDefaultAsync(m => m.Id == moodTypeId 
                                    && m.IsDeleted != true 
                                    && m.IsActive == true, cancellationToken);

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

        // Lấy giờ VN (UTC+7) để so sánh ngày
        var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowUTC = DateTime.UtcNow;
        var nowVN = TimeZoneInfo.ConvertTimeFromUtc(nowUTC, vnTimeZone);
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
            todayLog.UpdatedAt = nowUTC; // Lưu UTC vào database
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
                CreatedAt = nowUTC, // Lưu UTC vào database
                UpdatedAt = nowUTC, // Lưu UTC vào database
                IsDeleted = false
            });
            _logger.LogInformation($"➕ Created new mood log for today VN (MoodType: {moodType.Name})");
        }

        await _unitOfWork.SaveChangesAsync(); 

        _logger.LogInformation($"✅ User {userId} đã cập nhật mood type thành {moodType.Name} (ID: {moodType.Id})");

        // Trả về response
        return new UpdateMoodTypeResponse
        {
            MoodTypeId = moodType.Id,
            MoodTypeName = moodType.Name,
            // IconUrl = moodType.IconUrl,
            UpdatedAt = memberProfile.UpdatedAt ?? DateTime.UtcNow
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
