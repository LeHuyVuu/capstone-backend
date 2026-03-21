using capstone_backend.Business.DTOs.CoupleProfile;
using capstone_backend.Business.Exceptions;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using capstone_backend.Extensions;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class CoupleProfileService : ICoupleProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public CoupleProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Success, string Message, CoupleProfileDetailResponse? Data)> GetCoupleProfileDetailAsync(int memberId)
    {
        // Check if member exists
        var member = await _unitOfWork.MembersProfile.GetByIdAsync(memberId);
        if (member == null || member.IsDeleted == true)
        {
            return (false, "Không tìm thấy thông tin member", null);
        }

        // Get active couple profile
        var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(memberId);
        if (couple == null)
        {
            return (false, "Bạn chưa có cặp đôi", null);
        }

        // Load related data
        var coupleWithDetails = await _unitOfWork.CoupleProfiles.GetFirstAsync(
            c => c.id == couple.id,
            include: query => query
                .Include(c => c.MemberId1Navigation)
                    .ThenInclude(m => m.User)
                .Include(c => c.MemberId2Navigation)
                    .ThenInclude(m => m.User)
                .Include(c => c.CouplePersonalityType)
                .Include(c => c.CoupleMoodType)
        );

        if (coupleWithDetails == null)
        {
            return (false, "Không tìm thấy thông tin cặp đôi", null);
        }

        // Map to response
        var response = new CoupleProfileDetailResponse
        {
            CoupleId = coupleWithDetails.id,
            CoupleName = coupleWithDetails.CoupleName,
            StartDate = coupleWithDetails.StartDate,
            AniversaryDate = coupleWithDetails.AniversaryDate,
            BudgetMin = coupleWithDetails.BudgetMin,
            BudgetMax = coupleWithDetails.BudgetMax,
            TotalPoints = coupleWithDetails.TotalPoints,
            InteractionPoints = coupleWithDetails.InteractionPoints,
            Status = coupleWithDetails.Status,
            CreatedAt = coupleWithDetails.CreatedAt,
            UpdatedAt = coupleWithDetails.UpdatedAt,

            // Member 1
            MemberId1 = coupleWithDetails.MemberId1,
            Member1Name = coupleWithDetails.MemberId1Navigation?.FullName ?? "Unknown",
            Member1AvatarUrl = coupleWithDetails.MemberId1Navigation?.User?.AvatarUrl,
            Member1Gender = coupleWithDetails.MemberId1Navigation?.Gender,
            Member1DateOfBirth = coupleWithDetails.MemberId1Navigation?.DateOfBirth,

            // Member 2
            MemberId2 = coupleWithDetails.MemberId2,
            Member2Name = coupleWithDetails.MemberId2Navigation?.FullName ?? "Unknown",
            Member2AvatarUrl = coupleWithDetails.MemberId2Navigation?.User?.AvatarUrl,
            Member2Gender = coupleWithDetails.MemberId2Navigation?.Gender,
            Member2DateOfBirth = coupleWithDetails.MemberId2Navigation?.DateOfBirth,

            // Personality Type
            CouplePersonalityTypeId = coupleWithDetails.CouplePersonalityTypeId,
            CouplePersonalityTypeName = coupleWithDetails.CouplePersonalityType?.Name,
            CouplePersonalityTypeDescription = coupleWithDetails.CouplePersonalityType?.Description,

            // Mood Type
            CoupleMoodTypeId = coupleWithDetails.CoupleMoodTypeId,
            CoupleMoodTypeName = coupleWithDetails.CoupleMoodType?.Name,
            CoupleMoodTypeDescription = coupleWithDetails.CoupleMoodType?.Description
        };

        return (true, "Lấy thông tin cặp đôi thành công", response);
    }

    public async Task<(bool Success, string Message, CoupleProfileDetailResponse? Data)> UpdateCoupleProfileAsync(
        int memberId,
        UpdateCoupleProfileRequest request)
    {
        // Validate basic request
        ValidateUpdateCoupleProfileRequest(request);

        // Check if member exists
        var member = await _unitOfWork.MembersProfile.GetByIdAsync(memberId);
        if (member == null || member.IsDeleted == true)
        {
            return (false, "Không tìm thấy thông tin member", null);
        }

        // Get active couple profile
        var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(memberId);
        if (couple == null)
        {
            return (false, "Bạn chưa có cặp đôi", null);
        }

        // Edge case: Check if member is part of this couple
        if (couple.MemberId1 != memberId && couple.MemberId2 != memberId)
        {
            return (false, "Bạn không có quyền cập nhật cặp đôi này", null);
        }

        // Edge case: Cannot update if couple is not ACTIVE
        if (couple.Status != CoupleProfileStatus.ACTIVE.ToString())
        {
            return (false, $"Không thể cập nhật cặp đôi có trạng thái {couple.Status}", null);
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.CoupleName))
        {
            couple.CoupleName = request.CoupleName;
        }


        if (request.AniversaryDate.HasValue)
        {
            couple.AniversaryDate = request.AniversaryDate.Value;
        }

        if (request.BudgetMin.HasValue)
        {
            couple.BudgetMin = request.BudgetMin.Value;
        }

        if (request.BudgetMax.HasValue)
        {
            couple.BudgetMax = request.BudgetMax.Value;
        }

        couple.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CoupleProfiles.Update(couple);
        await _unitOfWork.SaveChangesAsync();

        // Return updated couple profile
        return await GetCoupleProfileDetailAsync(memberId);
    }

    private void ValidateUpdateCoupleProfileRequest(UpdateCoupleProfileRequest request)
    {
        // At least one field must be provided
        if (string.IsNullOrEmpty(request.CoupleName) &&
            !request.AniversaryDate.HasValue &&
            !request.BudgetMin.HasValue &&
            !request.BudgetMax.HasValue)
        {
            throw new BadRequestException(
                "Phải cung cấp ít nhất một trường để cập nhật",
                "VALIDATION_ERROR");
        }

        // CoupleName validation
        if (!string.IsNullOrEmpty(request.CoupleName) && request.CoupleName.Length > 100)
        {
            throw new BadRequestException(
                "Tên cặp đôi không được vượt quá 100 ký tự",
                "VALIDATION_ERROR");
        }

        // StartDate validation
     

       if (request.AniversaryDate.HasValue && request.AniversaryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
{
    throw new BadRequestException(
        "Ngày kỷ niệm lớn hơn ngày hiện tại",
        "VALIDATION_ERROR");
}


        // Budget validation
        if (request.BudgetMin.HasValue && request.BudgetMin.Value < 0)
        {
            throw new BadRequestException(
                "Ngân sách tối thiểu phải lớn hơn hoặc bằng 0",
                "VALIDATION_ERROR");
        }

        if (request.BudgetMax.HasValue && request.BudgetMax.Value < 0)
        {
            throw new BadRequestException(
                "Ngân sách tối đa phải lớn hơn hoặc bằng 0",
                "VALIDATION_ERROR");
        }

        if (request.BudgetMin.HasValue && request.BudgetMax.HasValue &&
            request.BudgetMax.Value < request.BudgetMin.Value)
        {
            throw new BadRequestException(
                "Ngân sách tối đa phải lớn hơn hoặc bằng ngân sách tối thiểu",
                "VALIDATION_ERROR");
        }
    }

  
}
