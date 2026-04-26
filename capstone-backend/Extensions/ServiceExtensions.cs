using Amazon.Rekognition;
using Amazon.Runtime;
using capstone_backend.Api.Filters;
using capstone_backend.Api.VenueRecommendation.Service;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Challenge;
using capstone_backend.Business.Jobs.Comment;
using capstone_backend.Business.Jobs.DatePlan;
using capstone_backend.Business.Jobs.Leaderboard;
using capstone_backend.Business.Jobs.Like;
using capstone_backend.Business.Jobs.Media;
using capstone_backend.Business.Jobs.Moderation;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Business.Jobs.Payment;
using capstone_backend.Business.Jobs.Review;
using capstone_backend.Business.Jobs.VenueSettlement;
using capstone_backend.Business.Jobs.VenueSubscription;
using capstone_backend.Business.Jobs.Voucher;
using capstone_backend.Business.Services;
using capstone_backend.Scripts;
using capstone_backend.Data.Context;
using capstone_backend.Data.Interfaces;
using capstone_backend.Data.Repositories;
using capstone_backend.Extensions.Common;
using FirebaseAdmin;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenAI.Moderations;
using Resend;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using capstone_backend.Business.Jobs.MemberSubscription;

namespace capstone_backend.Extensions;

/// <summary>
/// Extension methods để đăng ký services vào DI Container
/// Code đơn giản, dễ hiểu - mỗi method làm 1 việc rõ ràng
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Đăng ký Database Context với PostgreSQL
    /// </summary>
    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (string.IsNullOrEmpty(dbHost) ||
            string.IsNullOrEmpty(dbName) ||
            string.IsNullOrEmpty(dbUser) ||
            string.IsNullOrEmpty(dbPassword))
        {
            throw new Exception("[ERROR] Database environment variables are not fully configured");
        }

        var connectionString =
            $"Host={dbHost};" +
            $"Port={dbPort};" +
            $"Database={dbName};" +
            $"Username={dbUser};" +
            $"Password={dbPassword};";

        // Debug log (không log password)
        Console.WriteLine($"[INFO] DB Host: {dbHost}");
        Console.WriteLine($"[INFO] DB Name: {dbName}");
        Console.WriteLine($"[INFO] DB User: {dbUser}");
        Console.WriteLine($"[INFO] DB Port: {dbPort}");

        //services.AddDbContext<MyDbContext>(options =>
        //    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<MyDbContext>(options =>
            options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention());

        return services;
    }


    /// <summary>
    /// Đăng ký tất cả Repositories
    /// Mỗi khi thêm entity mới, thêm repository vào đây
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
        services.AddScoped<IMemberMoodLogRepository, MemberMoodLogRepository>();
        services.AddScoped<IMoodTypeRepository, MoodTypeRepository>();
        services.AddScoped<ICoupleProfileRepository, CoupleProfileRepository>();
        services.AddScoped<ICoupleInvitationRepository, CoupleInvitationRepository>();
        services.AddScoped<ITestTypeRepository, TestTypeRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
        services.AddScoped<IPersonalityTestRepository, PersonalityTestRepository>();
        services.AddScoped<IVenueLocationRepository, VenueLocationRepository>();
        services.AddScoped<ILocationTagRepository, LocationTagRepository>();
        services.AddScoped<IDatePlanRepository, DatePlanRepository>();
        services.AddScoped<IDatePlanItemRepository, DatePlanItemRepository>();
        services.AddScoped<IVenueOwnerProfileRepository, VenueOwnerProfileRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<IDatePlanJobRepository, DatePlanJobRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ICheckInHistoryRepository, CheckInHistoryRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IReviewReplyRepository, ReviewReplyRepository>();
        services.AddScoped<IReviewLikeRepository, ReviewLikeRepository>();
        services.AddScoped<IChallengeRepository, ChallengeRepository>();
        services.AddScoped<IAdvertisementRepository, AdvertisementRepository>();
        services.AddScoped<ISpecialEventRepository, SpecialEventRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostLikeRepository, PostLikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
        services.AddScoped<ICoupleProfileChallengeRepository, CoupleProfileChallengeRepository>();
        services.AddScoped<ICouplePersonalityTypeRepository, CouplePersonalityTypeRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IVoucherItemRepository, VoucherItemRepository>();
        services.AddScoped<IVoucherItemMemberRepository, VoucherItemMemberRepository>();
        services.AddScoped<IVoucherLocationRepository, VoucherLocationRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IVoucherJobRepository, VoucherJobRepository>();
        services.AddScoped<IVoucherItemJobRepository, VoucherItemJobRepository>();

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWithdrawRequestRepository, WithdrawRequestRepository>();

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ISubscriptionPackageRepository, SubscriptionPackageRepository>();
        services.AddScoped<IMemberSubscriptionPackageRepository, MemberSubscriptionPackageRepository>();
        services.AddScoped<IVenueSettlementRepository, VenueSettlementRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<IAccessoryRepository, AccessoryRepository>();
        services.AddScoped<IAccessoryPurchaseRepository, AccessoryPurchaseRepository>();
        services.AddScoped<IMemberAccessoryRepository, MemberAccessoryRepository>();

        // Messaging repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMemberRepository, ConversationMemberRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportTypeRepository, ReportTypeRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Đăng ký tất cả Business Services
    /// Mỗi khi thêm service mới, thêm vào đây
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();

        // Register AI Recommendation Services
        services.AddScoped<IMoodMappingService, MoodMappingService>();
        services.AddScoped<PersonalityMappingService>();
        services.AddScoped<IRecommendationService, OpenAIRecommendationService>();

        // Đăng ký AWS Rekognition Service để phân tích cảm xúc khuôn mặt
        services.AddAwsRekognitionService();

        // Đăng ký AWS S3 Service để upload files
        services.AddAwsS3Service();

        // Register new services
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IMoodTypeService, MoodTypeService>();
        services.AddScoped<ISearchHistoryService, SearchHistoryService>();
        services.AddScoped<ISpecialEventService, SpecialEventService>();
        services.AddScoped<ITestTypeService, TestTypeService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IPersonalityTestService, PersonalityTestService>();
        services.AddScoped<IVenueLocationService, VenueLocationService>();
        services.AddScoped<IDatePlanService, DatePlanService>();
        services.AddScoped<IDatePlanItemService, DatePlanItemService>();
        services.AddScoped<IMbtiContentService, MbtiContentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDeviceTokenService, DeviceTokenService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IVenueVoucherService, VenueVoucherService>();
        services.AddScoped<IMemberVoucherService, MemberVoucherService>();
        services.AddScoped<IAdminVoucherService, AdminVoucherService>();
        services.AddScoped<IVoucherItemService, VoucherItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IMomoService, MomoService>();
        services.AddScoped<IMemberSubscriptionService, MemberSubscriptionService>();
        services.AddScoped<IVenueSettlementService, VenueSettlementService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IAccessoryService, AccessoryService>();

        // Register Subscription Package Service
        services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();

        // Register Subscription Validation Service
        services.AddScoped<ISubscriptionValidationService, SubscriptionValidationService>();

        // Register Couple Invitation Service
        services.AddScoped<ICoupleInvitationService, CoupleInvitationService>();

        // Register Couple Profile Service
        services.AddScoped<ICoupleProfileService, CoupleProfileService>();

        // Register Venue Owner Dashboard Service
        services.AddScoped<IVenueOwnerDashboardService, VenueOwnerDashboardService>();

        // Register Venue Owner Profile Service
        services.AddScoped<IVenueOwnerProfileService, VenueOwnerProfileService>();

        // Register Hangfire Jobs
        services.AddScoped<IDatePlanWorker, DatePlanWorker>();
        services.AddScoped<IReviewWorker, ReviewWorker>();
        services.AddScoped<IMediaWorker, MediaWorker>();
        services.AddScoped<IModerationWorker, ModerationWorker>();
        services.AddScoped<ICommentWorker, CommentWorker>();
        services.AddScoped<ILikeWorker, LikeWorker>();
        services.AddScoped<IChallengeWorker, ChallengeWorker>();
        services.AddScoped<IVoucherWorker, VoucherWorker>();
        services.AddScoped<ILeaderboardWorker, LeaderboardWorker>();
        services.AddScoped<IVenueSettlementWorker, VenueSettlementWorker>();
        services.AddScoped<IPaymentWorker, PaymentWorker>();
        services.AddScoped<IVenueSubscriptionWorker, VenueSubscriptionWorker>();
        services.AddScoped<INotificationWorker, NotificationWorker>();
        services.AddScoped<IMemberSubscriptionWorker, MemberSubscriptionWorker>();

        // Register Messaging Service
        services.AddScoped<IMessagingService, MessagingService>();

        // Register Sepay Service for payment (generates VietQR codes + receives webhooks)
        services.AddScoped<SepayService>();

        services.AddScoped<IZaloPayService, ZaloPayService>();

        services.AddScoped<IVNPayService, VNPayService>();

        // Register Refund Service (reusable for all refund scenarios)
        services.AddScoped<RefundService>();

        // Register Advertisement Service
        services.AddScoped<IAdvertisementService, AdvertisementService>();

        services.AddScoped<IInteractionTrackingService, InteractionTrackingService>();

        services.AddScoped<ILocationTrackingService, LocationTrackingService>();

        services.AddOpenAIModerationService();

        // Register Meilisearch Service
        services.AddScoped<IMeilisearchService, MeilisearchService>();

        // Register Voucher Code Generator
        services.AddScoped<IVoucherCodeGenerator, VoucherCodeGenerator>();

        services.AddScoped<WalletService>();

        // Register Wallet Payment Service (for instant wallet payments)
        services.AddScoped<WalletPaymentService>();

        // Register Leaderboard Service
        services.AddScoped<ILeaderboardService, LeaderboardService>();

        // Register Report Service
        services.AddScoped<IReportService, ReportService>();

        // Register Report Type Service
        services.AddScoped<IReportTypeService, ReportTypeService>();

        // Register Interest Service
        services.AddScoped<IInterestService, InterestService>();

        // Register Venue Tag Analysis Service
        services.AddScoped<IVenueTagAnalysisService, VenueTagAnalysisService>();

        // Background listener for Firebase Realtime Database location updates
        services.AddHostedService<FirebaseLocationListenerHostedService>();

        return services;
    }

    /// <summary>
    /// Đăng ký AWS Rekognition Service
    /// Đọc credentials từ environment variables
    /// </summary>
    public static IServiceCollection AddAwsRekognitionService(this IServiceCollection services)
    {
        var accessKey = (Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "").Trim();
        var secretKey = (Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "").Trim();
        var region = (Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-2").Trim();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            throw new Exception("[ERROR] Missing AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY");

        Console.WriteLine($"[INFO] AWS Region: {region}");
        Console.WriteLine($"[INFO] AWS AccessKey: {accessKey.Substring(0, Math.Min(10, accessKey.Length))}...");

        var creds = new BasicAWSCredentials(accessKey, secretKey);

        services.AddSingleton<IAmazonRekognition>(sp =>
            new AmazonRekognitionClient(creds, Amazon.RegionEndpoint.GetBySystemName(region)));

        services.AddScoped<FaceEmotionService>();
        return services;
    }


    /// <summary>
    /// Đăng ký AWS S3 Service
    /// Đọc credentials từ environment variables
    /// </summary>
    public static IServiceCollection AddAwsS3Service(this IServiceCollection services)
    {
        // Đọc AWS credentials từ environment variables (ĐÚNG TÊN)
        var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-2";
        var s3BucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME");

        if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey))
            throw new Exception("[ERROR] Missing AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY for S3");

        if (string.IsNullOrWhiteSpace(s3BucketName))
            throw new Exception("[ERROR] Missing AWS_S3_BUCKET_NAME");

        Console.WriteLine($"🪣 S3 Bucket: {s3BucketName}");
        Console.WriteLine($"🌍 S3 Region: {awsRegion}");

        var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

        var s3Config = new Amazon.S3.AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
        };

        services.AddSingleton<Amazon.S3.IAmazonS3>(
            new Amazon.S3.AmazonS3Client(awsCredentials, s3Config)
        );

        // Bạn đang DI IS3Service/S3Service, OK
        services.AddScoped<S3StorageService>();

        return services;
    }


    /// <summary>
    /// Đăng ký FluentValidation để validate request
    /// </summary>
    public static IServiceCollection AddFluentValidationConfiguration(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }

    /// <summary>
    /// Register Authentication supporting both Cookie (Web) and JWT (Mobile)
    /// </summary>
    public static IServiceCollection AddHybridAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                     ?? configuration["Jwt:SecretKey"]
                     ?? throw new InvalidOperationException("JWT Secret Key is not configured");

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                        ?? configuration["Jwt:Issuer"]
                        ?? "CapstoneAPI";

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                          ?? configuration["Jwt:Audience"]
                          ?? "CapstoneApp";

        Console.WriteLine($"[INFO] Auth: Cookie (Web) + JWT (Mobile)");
        Console.WriteLine($"[INFO] JWT Issuer: {jwtIssuer}");
        Console.WriteLine($"[INFO] JWT Audience: {jwtAudience}");

        services.AddAuthentication(options =>
            {
                // Default scheme for Web is Cookie
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Cookie Authentication for Web
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "CapstoneAuth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            })
            // JWT Authentication for Mobile
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Register JWT Authentication for mobile and web (deprecated - use AddHybridAuthenticationConfiguration)
    /// </summary>
    public static IServiceCollection AddJwtAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                     ?? configuration["Jwt:SecretKey"]
                     ?? throw new InvalidOperationException("JWT Secret Key is not configured");

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                        ?? configuration["Jwt:Issuer"]
                        ?? "CapstoneAPI";

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                          ?? configuration["Jwt:Audience"]
                          ?? "CapstoneApp";

        Console.WriteLine($"🔐 JWT Issuer: {jwtIssuer}");
        Console.WriteLine($"🔐 JWT Audience: {jwtAudience}");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
                };

                // For handling JWT in both Authorization header and query string (optional)
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Allow token from query string for SignalR/WebSocket connections
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Register Cookie Authentication (legacy - for web only)
    /// </summary>
    public static IServiceCollection AddCookieAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "CapstoneAuth";
                options.Cookie.HttpOnly = true; // Bảo mật: không cho JS access
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Chỉ gửi qua HTTPS
                options.Cookie.SameSite = SameSiteMode.Strict; // Chống CSRF
                options.ExpireTimeSpan = TimeSpan.FromHours(8); // Cookie hết hạn sau 8 giờ
                options.SlidingExpiration = true; // Tự động gia hạn khi user active

                // API trả về 401 thay vì redirect
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Đăng ký Controllers với ValidationFilter
    /// ValidationFilter tự động bắt lỗi validation
    /// </summary>
    public static IServiceCollection AddValidationFilter(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        })
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter()
                    );
                // Ensure all DateTime values are serialized as UTC with 'Z' suffix
                opts.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                opts.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Let ValidationFilter shape validation errors to the custom response format.
            options.SuppressModelStateInvalidFilter = true;
        });

        return services;
    }

    /// <summary>
    /// Đăng ký CORS để cho phép frontend gọi API
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .SetIsOriginAllowed(_ => true) // allow tất cả origin
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Đăng ký Hangfire với PostgreSQL
    /// Dùng để chạy background jobs như cập nhật IsClosed status
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (string.IsNullOrEmpty(dbHost) ||
            string.IsNullOrEmpty(dbName) ||
            string.IsNullOrEmpty(dbUser) ||
            string.IsNullOrEmpty(dbPassword))
        {
            Console.WriteLine("[WARNING] Hangfire: Database environment variables not configured, skipping Hangfire setup");
            return services;
        }

        var hangfireConnectionString =
            $"Host={dbHost};" +
            $"Port={dbPort};" +
            $"Database={dbName};" +
            $"Username={dbUser};" +
            $"Password={dbPassword};";

        services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(hangfireConnectionString));
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
        });

        Console.WriteLine("[INFO] Hangfire: Configured with PostgreSQL");

        return services;
    }

    /// <summary>
    /// Register Redis Cache
    /// </summary>
    public static IServiceCollection AddRedisConfiguration(this IServiceCollection services)
    {
        // 1. Lấy config từ .env
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");
        var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
        var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
        var redisConnStr = $"{redisHost}:{redisPort}";

        // 2. Validate
        if (string.IsNullOrWhiteSpace(redisConnStr) || string.IsNullOrEmpty(redisPassword))
        {
            Console.WriteLine("[WARNING] REDIS var not found in .env. Redis functionality will be disabled.");
            return services;
        }

        try
        {
            var configuration = new ConfigurationOptions
            {
                EndPoints = { redisConnStr },
                Password = string.IsNullOrEmpty(redisPassword) ? null : redisPassword,
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ConnectTimeout = 5000,
                SyncTimeout = 5000
            };

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(configuration));

            services.AddSingleton<IRedisService, RedisService>();

            Console.WriteLine($"[INFO] Redis connected: {configuration.EndPoints.FirstOrDefault()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Redis connection failed: {ex.Message}");
        }

        return services;
    }

    // Register Firebase Service
    public static IServiceCollection AddFireBaseConfiguration(this IServiceCollection services)
    {
        var type = Environment.GetEnvironmentVariable("FIREBASE_TYPE");
        var tokenUri = Environment.GetEnvironmentVariable("FIREBASE_TOKEN_URI");

        var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
        var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");
        var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");

        if (string.IsNullOrWhiteSpace(projectId) ||
            string.IsNullOrWhiteSpace(privateKey) ||
            string.IsNullOrWhiteSpace(clientEmail))
        {
            Console.WriteLine("[WARNING] Firebase: Environment variables not configured, skipping Firebase setup");
            return services;
        }

        try
        {
            // Check if config is valid
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(privateKey))
            {
                Console.WriteLine("[WARNING] Firebase config missing. FCM will be disabled.");
                return services;
            }

            // Only init if instance not exists
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialJson = JsonSerializer.Serialize(new
                {
                    type = type,
                    project_id = projectId,
                    private_key = privateKey.Replace("\\n", "\n"),
                    client_email = clientEmail,
                    token_uri = tokenUri
                });

                var credential = GoogleCredential.FromJson(credentialJson);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = projectId
                });

                Console.WriteLine($"[INFO] Firebase initialized for project: {projectId}");
            }
            else
            {
                Console.WriteLine("[INFO] Firebase already initialized");
            }

            services.AddScoped<IFcmService, FcmService>();

            return services;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Firebase initialization failed: {ex.Message}");
        }

        return services;
    }

    /// <summary>
    /// Register OpenAI Service
    /// </summary>
    public static IServiceCollection AddOpenAIModerationService(this IServiceCollection services)
    {
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(openAiKey))
        {
            Console.WriteLine("[WARNING] OpenAI API Key not found. Moderation AI will be disabled.");
            return services;
        }

        try
        {
            services.AddSingleton(new ModerationClient("omni-moderation-latest", openAiKey));
            // Register Moderation Service
            services.AddSingleton<IModerationService, ModerationService>();

            Console.WriteLine("[INFO] OpenAI Moderation AI: Initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OpenAI initialization failed: {ex.Message}");
        }

        return services;
    }

    /// <summary>
    /// Register Resend Email Service
    /// </summary>
    public static IServiceCollection AddEmailConfiguration(this IServiceCollection services)
    {
        var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");

        if (string.IsNullOrWhiteSpace(resendApiKey))
        {
            Console.WriteLine("[WARNING] Resend API Key not found. Email Service will not work.");
            return services;
        }

        try
        {
            services.AddOptions();
            services.Configure<ResendClientOptions>(o =>
            {
                o.ApiToken = resendApiKey;
            });
            services.AddHttpClient<ResendClient>();
            services.AddTransient<IResend, ResendClient>();

            // Inject interface
            services.AddScoped<IEmailService, EmailService>();

            Console.WriteLine("[INFO] Resend Email Service: Initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Resend Email initialization failed: {ex.Message}");
        }

        return services;
    }
}