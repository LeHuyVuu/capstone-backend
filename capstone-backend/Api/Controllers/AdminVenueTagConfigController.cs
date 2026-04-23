using capstone_backend.Api.Models;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.SystemConfig;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// Admin controller để quản lý config cho venue tag analysis
/// </summary>
[ApiController]
[Route("api/admin/venue-tag-config")]
[Authorize(Roles = "ADMIN")]
public class AdminVenueTagConfigController : BaseController
{
    private readonly ISystemConfigService _systemConfigService;

    public AdminVenueTagConfigController(ISystemConfigService systemConfigService)
    {
        _systemConfigService = systemConfigService;
    }

    /// <summary>
    /// Lấy các config hiện tại cho venue tag analysis
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVenueTagAnalysisConfig()
    {
        var goodThreshold = await GetConfigOrDefaultAsync(
            VenueTagAnalysisConstants.GOOD_THRESHOLD_KEY,
            VenueTagAnalysisConstants.DEFAULT_GOOD_THRESHOLD);

        var warningThreshold = await GetConfigOrDefaultAsync(
            VenueTagAnalysisConstants.WARNING_THRESHOLD_KEY,
            VenueTagAnalysisConstants.DEFAULT_WARNING_THRESHOLD);

        var minReviews = await GetConfigOrDefaultAsync(
            VenueTagAnalysisConstants.MIN_REVIEWS_KEY,
            VenueTagAnalysisConstants.DEFAULT_MIN_REVIEWS);

        var result = new
        {
            GoodThreshold = goodThreshold,
            WarningThreshold = warningThreshold,
            MinReviews = minReviews,
            Description = new
            {
                GoodThreshold = $">= {goodThreshold}% = Tag phù hợp (GOOD)",
                WarningThreshold = $"{warningThreshold}% - {goodThreshold - 0.1m}% = Cần xem xét (WARNING)",
                Poor = $"< {warningThreshold}% = Không phù hợp (POOR)",
                MinReviews = $"Cần ít nhất {minReviews} reviews để đánh giá"
            }
        };

        return OkResponse(result, "Lấy config thành công");
    }

    /// <summary>
    /// Cập nhật config cho venue tag analysis
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateVenueTagAnalysisConfig(
        [FromBody] UpdateVenueTagAnalysisConfigRequest request)
    {
        var updatedConfigs = new List<string>();

        if (request.GoodThreshold.HasValue)
        {
            await _systemConfigService.UpdateConfigAsync(new UpdateSystemConfigRequest
            {
                ConfigKey = VenueTagAnalysisConstants.GOOD_THRESHOLD_KEY,
                ConfigValue = request.GoodThreshold.Value.ToString()
            });
            updatedConfigs.Add($"GoodThreshold = {request.GoodThreshold.Value}%");
        }

        if (request.WarningThreshold.HasValue)
        {
            await _systemConfigService.UpdateConfigAsync(new UpdateSystemConfigRequest
            {
                ConfigKey = VenueTagAnalysisConstants.WARNING_THRESHOLD_KEY,
                ConfigValue = request.WarningThreshold.Value.ToString()
            });
            updatedConfigs.Add($"WarningThreshold = {request.WarningThreshold.Value}%");
        }

        if (request.MinReviews.HasValue)
        {
            await _systemConfigService.UpdateConfigAsync(new UpdateSystemConfigRequest
            {
                ConfigKey = VenueTagAnalysisConstants.MIN_REVIEWS_KEY,
                ConfigValue = request.MinReviews.Value.ToString()
            });
            updatedConfigs.Add($"MinReviews = {request.MinReviews.Value}");
        }

        return OkResponse(new
        {
            UpdatedConfigs = updatedConfigs
        }, "Cập nhật config thành công");
    }

    private async Task<decimal> GetConfigOrDefaultAsync(string key, decimal defaultValue)
    {
        try
        {
            return await _systemConfigService.GetDecimalValueAsync(key);
        }
        catch
        {
            return defaultValue;
        }
    }

    private async Task<int> GetConfigOrDefaultAsync(string key, int defaultValue)
    {
        try
        {
            return await _systemConfigService.GetIntValueAsync(key);
        }
        catch
        {
            return defaultValue;
        }
    }
}
