using capstone_backend.Api.Filters;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data;
using capstone_backend.Data.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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
    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Đăng ký tất cả Repositories
    /// Mỗi khi thêm entity mới, thêm repository vào đây
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Đăng ký từng repository
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Đăng ký UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Đăng ký tất cả Business Services
    /// Mỗi khi thêm service mới, thêm vào đây
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        
        // Thêm services khác ở đây khi cần
        // services.AddScoped<IProductService, ProductService>();

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
    /// Đăng ký Cookie Authentication (đăng nhập bằng cookie)
    /// </summary>
    public static IServiceCollection AddCookieAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "CapstoneAuth";
                options.Cookie.HttpOnly = true;  // Bảo mật: không cho JS access
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Chỉ gửi qua HTTPS
                options.Cookie.SameSite = SameSiteMode.Strict;  // Chống CSRF
                options.ExpireTimeSpan = TimeSpan.FromHours(8);  // Cookie hết hạn sau 8 giờ
                options.SlidingExpiration = true;  // Tự động gia hạn khi user active
                
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
        // Đọc danh sách origins từ appsettings.json
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                           ?? new[] { "http://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)  // Chỉ cho phép origins này
                    .AllowAnyMethod()             // Cho phép tất cả methods (GET, POST, PUT...)
                    .AllowAnyHeader()             // Cho phép tất cả headers
                    .AllowCredentials();          // Cho phép gửi cookie
            });
        });

        return services;
    }
}
