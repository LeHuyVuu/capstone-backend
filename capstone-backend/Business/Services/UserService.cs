using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Auth;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;

namespace capstone_backend.Business.Services;

/// <summary>
/// User service - handles all user-related business logic
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IJwtService _jwtService;
    private readonly ICollectionService _collectionService;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IAccessoryService _accessoryService;
    private readonly IMemberSubscriptionService _memberSubscriptionService;

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IJwtService jwtService,
        ICollectionService collectionService,
        IRedisService redisService,
        IEmailService emailService,
        IGoogleAuthService googleAuthService,
        IAccessoryService accessoryService,
        IMemberSubscriptionService memberSubscriptionService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _jwtService = jwtService;
        _collectionService = collectionService;
        _redisService = redisService;
        _emailService = emailService;
        _googleAuthService = googleAuthService;
        _accessoryService = accessoryService;
        _memberSubscriptionService = memberSubscriptionService;
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
        var dateOfBirth = memberProfile?.DateOfBirth;
        var inviteCode = memberProfile?.InviteCode;

        // Generate  JWT tokens
        var role = user.Role ?? "MEMBER";
        var fullName = user.DisplayName ?? string.Empty;
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, role, fullName, user.AssignedVenueLocationId);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        var balance = user.Wallet?.IsActive == true ? user.Wallet.Balance.Value : 0;
        var points = user.Wallet?.IsActive == true ? user.Wallet.Points.Value : 0;

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Gender = gender,
            AvatarUrl = user.AvatarUrl,
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            InviteCode = inviteCode,
            Balance = balance,
            Points = points
        };
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra email đã tồn tại
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng");

        // Tạo user account với BCrypt hashing
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
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

            await CreateWalletForUserAsync(user.Id);

            // Tạo member profile
            await CreateMemberProfileAsync(user.Id, request);

            // Auto kích hoạt gói default
            await _memberSubscriptionService.EnsureDefaultSubscriptionAsync(user.Id);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, "MEMBER", request.FullName, null);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
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
        memberProfile.InviteCode = await GenerateInviteCode();

        await _unitOfWork.Context.Set<MemberProfile>().AddAsync(memberProfile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created member profile for user {UserId} with invite code {InviteCode}",
            userId, memberProfile.InviteCode);

        // Tạo collection mặc định "Mục yêu thích" cho member mới
        await _collectionService.CreateDefaultCollectionForMemberAsync(memberProfile.Id);
        _logger.LogInformation("Created default collection for member {MemberId}", memberProfile.Id);
    }

    /// <summary>
    /// Generate unique 6-character invite code (checks database for uniqueness)
    /// </summary>
    private async Task<string> GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string inviteCode;
        bool isUnique = false;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            inviteCode = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());

            // Kiểm tra mã đã tồn tại trong database
            var existingProfile = await _unitOfWork.Context.Set<MemberProfile>()
                .FirstOrDefaultAsync(mp => mp.InviteCode == inviteCode);

            isUnique = existingProfile == null;
            attempts++;

        } while (!isUnique && attempts < maxAttempts);

        if (!isUnique)
            throw new InvalidOperationException("Unable to generate unique invite code after multiple attempts");

        return inviteCode;
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

        await CreateWalletForUserAsync(user.Id);

        // Tạo venue owner profile
        await CreateVenueOwnerProfileAsync(user.Id, request);

        // Generate JWT tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, "venueowner", request.BusinessName, null);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        _logger.LogInformation("✅ VenueOwner registered successfully: {Email} (UserId: {UserId})", user.Email, user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
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
        return user == null ? null : await MapToUserResponse(user);
    }

    public async Task<UserResponse?> GetUserByIdAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdWithProfilesAsync(userId); // Reuse the same method since it already includes profiles
        return user == null ? null : await MapToUserResponse(user);
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

        var items = new List<UserResponse>();
        foreach (var user in users)
        {
            items.Add(await MapToUserResponse(user));
        }

        return new PagedResult<UserResponse>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request, int? createdBy = null)
    {
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' đã tồn tại");

        var normalizedRole = request.Role?.Trim().ToUpperInvariant();
        if (normalizedRole != "STAFF")
            throw new InvalidOperationException("Endpoint này chỉ hỗ trợ tạo role STAFF");

        if (!request.LocationId.HasValue)
            throw new InvalidOperationException("LocationId là bắt buộc khi tạo STAFF");

        var venueLocation = await _unitOfWork.VenueLocations.GetByIdAsync(request.LocationId.Value);
        if (venueLocation == null || venueLocation.IsDeleted == true)
            throw new InvalidOperationException("Venue location không tồn tại");

        if (!string.Equals(venueLocation.Status, VenueLocationStatus.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ có thể gán STAFF vào venue location đang ACTIVE");

        int? assignedVenueLocationId = venueLocation.Id;

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new UserAccount
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Role = normalizedRole,
            IsActive = true,
            IsVerified = false,
            IsDeleted = false,
            AssignedVenueLocationId = assignedVenueLocationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await CreateWalletForUserAsync(user.Id);

        return await MapToUserResponse(user);
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
        {
            var normalizedRole = request.Role.Trim().ToUpperInvariant();
            if (normalizedRole is not ("ADMIN" or "MEMBER" or "VENUEOWNER" or "STAFF"))
                throw new InvalidOperationException("Role không hợp lệ. Chỉ chấp nhận: ADMIN, MEMBER, VENUEOWNER, STAFF");

            if (normalizedRole == "STAFF" && !user.AssignedVenueLocationId.HasValue)
                throw new InvalidOperationException("Không thể cập nhật role thành STAFF khi chưa có location được gán");

            if (normalizedRole != "STAFF")
                user.AssignedVenueLocationId = null;

            user.Role = normalizedRole;
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return await MapToUserResponse(user);
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
    private async Task<UserResponse> MapToUserResponse(UserAccount user)
    {
        var memberProfile = user.MemberProfiles?.FirstOrDefault(p => p.IsDeleted != true);
        var venueOwnerProfile = user.VenueOwnerProfiles?.FirstOrDefault(p => p.IsDeleted != true);

        var equippedAccessories = new List<EquippedAccessoryBriefResponse>();
        if (memberProfile != null)
            equippedAccessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(memberProfile.Id);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.DisplayName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role ?? "MEMBER",
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
                Interests = TryParseJson(memberProfile.Interests),
                AvailableTime = TryParseJson(memberProfile.AvailableTime),
                InviteCode = memberProfile.InviteCode,

                EquippedAccessories = equippedAccessories
            } : null,
            VenueOwnerProfile = venueOwnerProfile != null ? new VenueOwnerProfileResponse
            {
                Id = venueOwnerProfile.Id,
                BusinessName = venueOwnerProfile.BusinessName,
                PhoneNumber = venueOwnerProfile.PhoneNumber,
                Email = venueOwnerProfile.Email,
                Address = venueOwnerProfile.Address,
                CitizenIdFrontUrl = user.CitizenIdFrontUrl,
                CitizenIdBackUrl = user.CitizenIdBackUrl,
                BusinessLicenseUrl = user.BusinessLicenseUrl
            } : null
        };
    }

    /// <summary>
    /// Try to parse JSON string to object, return null if invalid
    /// </summary>
    private static object? TryParseJson(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(jsonString);
        }
        catch
        {
            return jsonString; // Return as-is if parsing fails
        }
    }

    /// <summary>
    /// Update password cho user đã đăng nhập
    /// </summary>
    public async Task<bool> UpdatePasswordAsync(int userId, UpdatePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Người dùng không tồn tại");

        // Verify current password
        bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng");

        // Hash new password
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        
        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated password successfully", userId);
        return true;
    }

    /// <summary>
    /// Gửi OTP qua email để reset password
    /// </summary>
    public async Task<bool> SendPasswordResetOtpAsync(ForgotPasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            throw new InvalidOperationException("Email không tồn tại trong hệ thống");

        if (user.IsActive != true)
            throw new InvalidOperationException("Tài khoản đã bị vô hiệu hóa");

        // Generate 6-digit OTP
        var random = new Random();
        string otpCode = random.Next(100000, 999999).ToString();

        // Store OTP in Redis with 10 minutes expiry
        string redisKey = $"otp:password-reset:{request.Email}";
        await _redisService.SetAsync(redisKey, otpCode, TimeSpan.FromMinutes(10));

        // Send OTP via email
        var emailRequest = new DTOs.Email.SendEmailRequest
        {
            To = request.Email,
            Subject = "Mã OTP đặt lại mật khẩu - CoupleMood",
            FromName = "CoupleMood",
            HtmlBody = EmailOtpTemplate.GetPasswordResetOtpEmail(otpCode, user.DisplayName ?? "Người dùng"),
            TextBody = EmailOtpTemplate.GetPasswordResetOtpPlainText(otpCode, user.DisplayName ?? "Người dùng")
        };

        bool emailSent = await _emailService.SendEmailAsync(emailRequest);
        if (!emailSent)
        {
            await _redisService.RemoveAsync(redisKey);
            throw new InvalidOperationException("Không thể gửi email. Vui lòng thử lại sau");
        }

        _logger.LogInformation("OTP sent to email {Email}", request.Email);
        return true;
    }

    /// <summary>
    /// Verify OTP code
    /// </summary>
    public async Task<bool> VerifyOtpAsync(VerifyOtpRequest request)
    {
        string redisKey = $"otp:password-reset:{request.Email}";
        var storedOtp = await _redisService.GetAsync<string>(redisKey);

        if (string.IsNullOrEmpty(storedOtp))
            throw new InvalidOperationException("Mã OTP không hợp lệ hoặc đã hết hạn");

        if (storedOtp != request.OtpCode)
            throw new InvalidOperationException("Mã OTP không chính xác");

        // Mark OTP as verified (store verified flag for 15 minutes)
        string verifiedKey = $"otp:verified:{request.Email}";
        await _redisService.SetAsync(verifiedKey, "true", TimeSpan.FromMinutes(15));

        _logger.LogInformation("OTP verified successfully for email {Email}", request.Email);
        return true;
    }

    /// <summary>
    /// Reset password sau khi verify OTP thành công
    /// </summary>
    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // Check if OTP was verified
        string verifiedKey = $"otp:verified:{request.Email}";
        var isVerified = await _redisService.GetAsync<string>(verifiedKey);

        if (string.IsNullOrEmpty(isVerified))
        {
            // Double check OTP one more time
            string otpKey = $"otp:password-reset:{request.Email}";
            var storedOtp = await _redisService.GetAsync<string>(otpKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != request.OtpCode)
                throw new InvalidOperationException("Vui lòng xác thực OTP trước khi đặt lại mật khẩu");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null)
            throw new InvalidOperationException("Người dùng không tồn tại");

        // Hash new password
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        
        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Clean up Redis keys
        await _redisService.RemoveAsync($"otp:password-reset:{request.Email}");
        await _redisService.RemoveAsync(verifiedKey);

        _logger.LogInformation("Password reset successfully for email {Email}", request.Email);
        return true;
    }

    /// <summary>
    /// Login hoặc register bằng Google (cho Flutter mobile)
    /// </summary>
    public async Task<LoginResponse?> GoogleLoginAsync(GoogleLoginRequest request)
    {
        return await GoogleLoginInternalAsync(request, isMobile: false);
    }

    /// <summary>
    /// Login hoặc register bằng Google (cho mobile)
    /// </summary>
    public async Task<LoginResponse?> GoogleMobileLoginAsync(GoogleLoginRequest request)
    {
        return await GoogleLoginInternalAsync(request, isMobile: true);
    }

    private async Task<LoginResponse?> GoogleLoginInternalAsync(GoogleLoginRequest request, bool isMobile)
    {
        // 1. Verify Google ID Token
        var googlePayload = isMobile
            ? await _googleAuthService.VerifyGoogleMobileTokenAsync(request.IdToken)
            : await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);

        if (googlePayload == null)
        {
            _logger.LogWarning("Invalid Google ID Token for channel: {Channel}", isMobile ? "mobile" : "web");
            return null;
        }

        var email = googlePayload.Email;
        var fullName = googlePayload.Name;
        var avatarUrl = googlePayload.Picture;

        // 2. Kiểm tra user đã tồn tại chưa
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(email);

        if (existingUser != null)
        {
            if (!isMobile && !IsAllowedWebGoogleRole(existingUser.Role))
            {
                _logger.LogWarning(
                    "Google web login blocked for email {Email} due to role {Role}",
                    email,
                    existingUser.Role ?? "<null>");
                throw new InvalidOperationException("Tài khoản này không có quyền đăng nhập web");
            }

            // User đã tồn tại - Login
            if (existingUser.IsActive != true)
            {
                _logger.LogWarning("User account is inactive: {Email}", email);
                return null;
            }

            // Update last login và avatar nếu có thay đổi
            existingUser.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(avatarUrl) && existingUser.AvatarUrl != avatarUrl)
            {
                existingUser.AvatarUrl = avatarUrl;
            }
            _unitOfWork.Users.Update(existingUser);
            await _unitOfWork.SaveChangesAsync();

            return await GenerateLoginResponse(existingUser);
        }

        // 3. User chưa tồn tại - Auto Register
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var newUser = new UserAccount
            {
                Email = email,
                DisplayName = fullName,
                AvatarUrl = avatarUrl,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                Role = "MEMBER",
                IsActive = true,
                IsVerified = true, // Google account đã verified
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            await CreateWalletForUserAsync(newUser.Id);

            // Tạo member profile với thông tin cơ bản
            var memberProfile = new MemberProfile
            {
                UserId = newUser.Id,
                FullName = fullName,
                RelationshipStatus = "SINGLE",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
                InviteCode = await GenerateInviteCode()
            };

            await _unitOfWork.Context.Set<MemberProfile>().AddAsync(memberProfile);
            await _unitOfWork.SaveChangesAsync();

            // Tạo collection mặc định
            await _collectionService.CreateDefaultCollectionForMemberAsync(memberProfile.Id);

            // Auto kích hoạt gói default
            await _memberSubscriptionService.EnsureDefaultSubscriptionAsync(newUser.Id);

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("New user registered via Google: {Email}", email);

            // Return response với memberProfile vừa tạo (không cần query lại)
            return GenerateLoginResponseFromData(newUser, memberProfile);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error during Google registration for {Email}", email);
            throw;
        }
    }

    private static bool IsAllowedWebGoogleRole(string? role)
    {
        return string.Equals(role, "VENUEOWNER", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "STAFF", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CreateWalletForUserAsync(int userId)
    {
        var existingWallet = await _unitOfWork.Wallets.GetByUserIdAsync(userId);
        if (existingWallet != null)
            return;

        await _unitOfWork.Wallets.AddAsync(new Wallet
        {
            UserId = userId,
            Balance = 0,
            Points = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Generate login response với JWT tokens (query member profile từ DB)
    /// </summary>
    private async Task<LoginResponse> GenerateLoginResponse(UserAccount user)
    {
        var memberProfile = user.MemberProfiles?.FirstOrDefault() 
                           ?? await _unitOfWork.Context.Set<MemberProfile>()
                               .FirstOrDefaultAsync(mp => mp.UserId == user.Id && mp.IsDeleted != true);

        return GenerateLoginResponseFromData(user, memberProfile);
    }

    /// <summary>
    /// Generate login response với JWT tokens (từ data có sẵn)
    /// </summary>
    private LoginResponse GenerateLoginResponseFromData(UserAccount user, MemberProfile? memberProfile)
    {
        var gender = memberProfile?.Gender ?? string.Empty;
        var dateOfBirth = memberProfile?.DateOfBirth;
        var inviteCode = memberProfile?.InviteCode;

        var role = user.Role ?? "MEMBER";
        var fullName = user.DisplayName ?? string.Empty;
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, role, fullName, user.AssignedVenueLocationId);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Gender = gender,
            AvatarUrl = user.AvatarUrl,
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            InviteCode = inviteCode
        };
    }
}