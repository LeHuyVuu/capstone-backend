using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.Entities;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services;

// Service xử lý logic về User
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Đăng nhập
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user == null || !user.IsActive)
            return null;

        // TODO: Thay bằng BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)
        bool isPasswordValid = request.Password == "temp";

        if (!isPasswordValid)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    // Lấy thông tin user hiện tại
    public async Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    // Lấy user theo ID
    public async Task<UserResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        return user == null ? null : MapToUserResponse(user);
    }

    // Lấy danh sách user có phân trang
    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        int pageNumber, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _unitOfWork.Users.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrEmpty(searchTerm) ? null : u => u.Email.Contains(searchTerm) || u.FullName.Contains(searchTerm),
            orderBy: query => query.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResult<UserResponse>(users.Select(MapToUserResponse), pageNumber, pageSize, totalCount);
    }

    // Tạo user mới
    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request, Guid? createdBy = null, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
            throw new InvalidOperationException($"Email '{request.Email}' đã tồn tại");

        // TODO: Thay bằng BCrypt.Net.BCrypt.HashPassword(request.Password)
        string passwordHash = "hashed_" + request.Password;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            IsActive = true,
            CreatedBy = createdBy
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    // Cập nhật user
    public async Task<UserResponse?> UpdateUserAsync(
        Guid userId, UpdateUserRequest request, Guid? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null) return null;

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedBy = updatedBy;
        if (!string.IsNullOrEmpty(request.Role)) user.Role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    // Xóa user (soft delete)
    public async Task<bool> DeleteUserAsync(
        Guid userId, Guid? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null) return false;

        _unitOfWork.Users.SoftDelete(user, deletedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Chuyển đổi User entity sang UserResponse DTO
    private static UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
