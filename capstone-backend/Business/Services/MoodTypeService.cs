using System.Text.Json;
using capstone_backend.Business.DTOs.MoodType;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class MoodTypeService : IMoodTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public MoodTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
