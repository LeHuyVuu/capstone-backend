using Amazon.Rekognition;
using Amazon.Runtime;
using capstone_backend.Api.Filters;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using capstone_backend.Data.Context;
using capstone_backend.Data.Interfaces;
using System.Text.Json.Serialization;

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

        services.AddDbContext<MyDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

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
        services.AddScoped<ITestTypeRepository, TestTypeRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
        services.AddScoped<IPersonalityTestRepository, PersonalityTestRepository>();
        services.AddScoped<IVenueLocationRepository, VenueLocationRepository>();
        services.AddScoped<ILocationTagRepository, LocationTagRepository>();
        services.AddScoped<IDatePlanRepository, DatePlanRepository>();

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

        // Register CometChat Service
        services.AddScoped<ICometChatService, CometChatService>();

        // Register AI Recommendation Services
        services.AddScoped<IMoodMappingService, MoodMappingService>();
        services.AddScoped<IPersonalityMappingService, PersonalityMappingService>();
        services.AddScoped<IVenueScoringEngine, VenueScoringEngine>();
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

        // Register Location Tracking Service (đơn giản, chỉ quản lý watchlist)
        services.AddScoped<ILocationFollowerService, LocationFollowerService>();

        // Register Subscription Package Service
        services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();

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
        services.AddControllers(options => { options.Filters.Add<ValidationFilter>(); })
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter()
                    );
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
}