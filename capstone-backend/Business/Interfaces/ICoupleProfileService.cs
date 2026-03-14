using capstone_backend.Business.DTOs.CoupleProfile;

namespace capstone_backend.Business.Interfaces;

public interface ICoupleProfileService
{
    /// <summary>
    /// Lấy chi tiết couple profile của member hiện tại
    /// </summary>
    Task<(bool Success, string Message, CoupleProfileDetailResponse? Data)> GetCoupleProfileDetailAsync(int memberId);

    /// <summary>
    /// Cập nhật thông tin couple profile
    /// </summary>
    Task<(bool Success, string Message, CoupleProfileDetailResponse? Data)> UpdateCoupleProfileAsync(
        int memberId, 
        UpdateCoupleProfileRequest request);
}
