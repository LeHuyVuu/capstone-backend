using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.Interfaces;
using capstone_backend.Entities;

namespace capstone_backend.Business.Services;

/// <summary>
/// User service - handles all user-related business logic
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user == null || user.is_active != true)
            return null;

        // TODO: Use BCrypt.Net.BCrypt.Verify(request.Password, user.password_hash)
        bool isPasswordValid = request.Password == "temp";

        if (!isPasswordValid)
            return null;

        user.last_login_at = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            UserId = user.id,
            Email = user.email,
            FullName = user.display_name ?? string.Empty,
            Role = user.role ?? "User"
        };
    }

    public async Task<UserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<UserResponse?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        return user == null ? null : MapToUserResponse(user);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        int pageNumber, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await _unitOfWork.Users.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrEmpty(searchTerm) 
                ? u => u.is_deleted != true
                : u => u.is_deleted != true && (u.email.Contains(searchTerm) || (u.display_name != null && u.display_name.Contains(searchTerm))),
            orderBy: query => query.OrderByDescending(u => u.created_at),
            cancellationToken: cancellationToken);

        return new PagedResult<UserResponse>(
            users.Select(MapToUserResponse), 
            pageNumber, 
            pageSize, 
            totalCount);
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request, int? createdBy = null, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
            throw new InvalidOperationException($"Email '{request.Email}' already exists");

        // TODO: Use BCrypt.Net.BCrypt.HashPassword(request.Password)
        string passwordHash = "hashed_" + request.Password;

        var user = new user_account
        {
            email = request.Email,
            password_hash = passwordHash,
            display_name = request.FullName,
            phone_number = request.PhoneNumber,
            role = request.Role,
            is_active = true,
            is_verified = false,
            is_deleted = false,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    public async Task<UserResponse?> UpdateUserAsync(
        int userId, UpdateUserRequest request, int? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null) return null;

        user.display_name = request.FullName;
        user.phone_number = request.PhoneNumber;
        user.is_active = request.IsActive;
        user.updated_at = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(request.Role)) 
            user.role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToUserResponse(user);
    }

    public async Task<bool> DeleteUserAsync(
        int userId, int? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
        if (user == null) return false;

        _unitOfWork.Users.SoftDelete(user, deletedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Convert user_account entity to UserResponse DTO
    /// </summary>
    private static UserResponse MapToUserResponse(user_account user)
    {
        return new UserResponse
        {
            Id = user.id,
            Email = user.email,
            FullName = user.display_name ?? string.Empty,
            PhoneNumber = user.phone_number,
            Role = user.role ?? "User",
            IsActive = user.is_active ?? false,
            LastLoginAt = user.last_login_at,
            CreatedAt = user.created_at ?? DateTime.MinValue,
            UpdatedAt = user.updated_at
        };
    }
}
