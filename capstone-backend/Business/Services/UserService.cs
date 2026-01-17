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
    private readonly IJwtService _jwtService;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user == null || user.is_active != true)
            return null;

        // Verify password
        // TODO: Use BCrypt.Net.BCrypt.Verify(request.Password, user.password_hash)
        // For now, compare with hashed password (temporary - should use BCrypt)
        bool isPasswordValid = user.password_hash == ("hashed_" + request.Password);

        if (!isPasswordValid)
            return null;

        user.last_login_at = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT tokens
        var role = user.role ?? "member";
        var fullName = user.display_name ?? string.Empty;
        var accessToken = _jwtService.GenerateAccessToken(user.id, user.email, role, fullName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        return new LoginResponse
        {
          
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Kiểm tra email đã tồn tại
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng");

        // Tạo user account
        // TODO: Use BCrypt.Net.BCrypt.HashPassword(request.Password)
        string passwordHash = "hashed_" + request.Password;

        var user = new user_account
        {
            email = request.Email,
            password_hash = passwordHash,
            display_name = request.FullName,
            phone_number = request.PhoneNumber,
            role = "member", // Luôn là member (lowercase theo database constraint)
            is_active = true,
            is_verified = false,
            is_deleted = false,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
            last_login_at = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Tạo member profile
        await CreateMemberProfileAsync(user.id, request, cancellationToken);

        // Generate JWT tokens
        var accessToken = _jwtService.GenerateAccessToken(user.id, user.email, "member", request.FullName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    /// <summary>
    /// Tạo member profile cho user
    /// </summary>
    private async Task CreateMemberProfileAsync(int userId, RegisterRequest request, CancellationToken cancellationToken)
    {
        var memberProfile = new member_profile
        {
            user_id = userId,
            full_name = request.FullName,
            date_of_birth = request.DateOfBirth,
            gender = request.Gender,
            relationship_status = "single", // Default (lowercase)
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
            is_deleted = false
        };

        // Generate unique invite code
        memberProfile.invite_code = GenerateInviteCode();

        await _unitOfWork.Context.Set<member_profile>().AddAsync(memberProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created member profile for user {UserId} with invite code {InviteCode}", 
            userId, memberProfile.invite_code);
    }

    /// <summary>
    /// Generate unique 6-character invite code
    /// </summary>
    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
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
