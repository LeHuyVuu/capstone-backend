using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services;

/// <summary>
/// User service - handles all user-related business logic
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IJwtService _jwtService;
    private readonly ICometChatService _cometChatService;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger, IJwtService jwtService, ICometChatService cometChatService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jwtService = jwtService;
        _cometChatService = cometChatService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

        if (user == null || user.IsActive != true)
            return null;

        // Verify password using BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Get member profile for gender
        var memberProfile = user.MemberProfiles?.FirstOrDefault();
        Console.Write(memberProfile);
        var gender = memberProfile?.Gender ?? string.Empty;

        // Generate JWT tokens
        var role = user.Role ?? "member";
        var fullName = user.DisplayName ?? string.Empty;
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, role, fullName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        // CometChat integration: Ensure user exists and generate auth token
        string cometChatUid = string.Empty;
        string cometChatAuthToken = string.Empty;
        if (user.Role == "member")
        {
            try
            {
                cometChatUid = await _cometChatService.EnsureCometChatUserExistsAsync(user.Email, fullName);
                cometChatAuthToken = await _cometChatService.GenerateCometChatAuthTokenAsync(cometChatUid);
                _logger.LogInformation("CometChat integration successful for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CometChat integration failed for user {UserId}. Continuing with login.", user.Id);
            }
        }

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CometChatUid = cometChatUid,
            CometChatAuthToken = cometChatAuthToken,
            Gender = gender
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra email đã tồn tại
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng");

        // Tạo user account với BCrypt hashing
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new UserAccount()
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = "MEMBER", // Luôn là member (lowercase theo database constraint)
            IsActive = true,
            IsVerified = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Tạo member profile
        await CreateMemberProfileAsync(user.Id, request);

        // Generate JWT tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, "member", request.FullName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        // CometChat integration: Create user and generate auth token
        string cometChatUid = string.Empty;
        string cometChatAuthToken = string.Empty;
        try
        {
            cometChatUid = await _cometChatService.CreateCometChatUserAsync(user.Email, request.FullName);
            cometChatAuthToken = await _cometChatService.GenerateCometChatAuthTokenAsync(cometChatUid);
            _logger.LogInformation("CometChat user created successfully for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CometChat user creation failed for user {UserId}. Continuing with registration.", user.Id);
            // Don't fail registration if CometChat fails - user can still use the app
        }

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CometChatUid = cometChatUid,
            CometChatAuthToken = cometChatAuthToken
        };
    }

    /// <summary>
    /// Tạo member profile cho user
    /// </summary>
    private async Task CreateMemberProfileAsync(int userId, RegisterRequest request)
    {
        var memberProfile = new MemberProfile
        {
            UserId = userId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            RelationshipStatus = "SINGLE", // Default (uppercase)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Generate unique invite code
        memberProfile.InviteCode = GenerateInviteCode();

        await _unitOfWork.Context.Set<MemberProfile>().AddAsync(memberProfile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created member profile for user {UserId} with invite code {InviteCode}",
            userId, memberProfile.InviteCode);
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

    /// <summary>
    /// Register VenueOwner account with venue_owner_profile
    /// </summary>
    public async Task<LoginResponse> RegisterVenueOwnerAsync(RegisterVenueOwnerRequest request)
    {
        // Kiểm tra email đã tồn tại
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng");

        // Tạo user account với BCrypt hashing
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new UserAccount()
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.BusinessName,
            PhoneNumber = request.PhoneNumber,
            Role = "VENUEOWNER", // Role là venue owner
            IsActive = true,
            IsVerified = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Tạo venue owner profile
        await CreateVenueOwnerProfileAsync(user.Id, request);

        // Generate JWT tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, "venueowner", request.BusinessName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        _logger.LogInformation("✅ VenueOwner registered successfully: {Email} (UserId: {UserId})", user.Email, user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CometChatUid = string.Empty,
            CometChatAuthToken = string.Empty
        };
    }

    /// <summary>
    /// Tạo venue owner profile cho user
    /// </summary>
    private async Task CreateVenueOwnerProfileAsync(int userId, RegisterVenueOwnerRequest request)
    {
        var venueOwnerProfile = new VenueOwnerProfile
        {
            UserId = userId,
            BusinessName = request.BusinessName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<VenueOwnerProfile>().AddAsync(venueOwnerProfile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("✅ Created venue owner profile for user {UserId} - Business: {BusinessName}",
            userId, request.BusinessName);
    }

    public async Task<UserResponse?> GetCurrentUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId);
        return user == null ? null : MapToUserResponse(user);
    }

    public async Task<UserResponse?> GetUserByIdAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user == null ? null : MapToUserResponse(user);
    }

    public async Task<PagedResult<UserResponse>> GetUsersAsync(
        int pageNumber, int pageSize, string? searchTerm = null)
    {
        var (users, totalCount) = await _unitOfWork.Users.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: string.IsNullOrEmpty(searchTerm)
                ? u => u.IsDeleted != true
                : u => u.IsDeleted != true && (u.Email.Contains(searchTerm) || (u.DisplayName != null && u.DisplayName.Contains(searchTerm))),
            orderBy: query => query.OrderByDescending(u => u.CreatedAt));

        return new PagedResult<UserResponse>(
            users.Select(MapToUserResponse),
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request, int? createdBy = null)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' already exists");

        // TODO: Use BCrypt.Net.BCrypt.HashPassword(request.Password)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new UserAccount
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            IsActive = true,
            IsVerified = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToUserResponse(user);
    }

    public async Task<UserResponse?> UpdateUserAsync(
        int userId, UpdateUserRequest request, int? updatedBy = null)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        user.DisplayName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Role))
            user.Role = request.Role;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return MapToUserResponse(user);
    }

    public async Task<bool> DeleteUserAsync(
        int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        user.IsDeleted = true;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Convert user_account entity to UserResponse DTO
    /// </summary>
    private static UserResponse MapToUserResponse(UserAccount user)
    {
        var memberProfile = user.MemberProfiles?.FirstOrDefault(p => p.IsDeleted != true);
        var venueOwnerProfile = user.VenueOwnerProfiles?.FirstOrDefault(p => p.IsDeleted != true);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.DisplayName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role ?? "User",
            IsActive = user.IsActive ?? false,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = user.UpdatedAt,
            MemberProfile = memberProfile != null ? new MemberProfileResponse
            {
                Id = memberProfile.Id,
                FullName = memberProfile.FullName,
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
                InviteCode = memberProfile.InviteCode
            } : null,
            VenueOwnerProfile = venueOwnerProfile != null ? new VenueOwnerProfileResponse
            {
                Id = venueOwnerProfile.Id,
                BusinessName = venueOwnerProfile.BusinessName,
                PhoneNumber = venueOwnerProfile.PhoneNumber,
                Email = venueOwnerProfile.Email,
                Address = venueOwnerProfile.Address
            } : null
        };
    }
}
