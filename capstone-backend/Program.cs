using capstone_backend.Api.Middleware;
using capstone_backend.Api.Models;
using capstone_backend.Business.Mappings;
using capstone_backend.Extensions;
using DotNetEnv;
using Scalar.AspNetCore;

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"[INFO] Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine($"[INFO] .env file not found at: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Configure Services (Dependency Injection)
// ========================================

// 1. Database Context with PostgreSQL + EF Core
builder.Services.AddDatabaseContext(builder.Configuration);

// 2. HttpClient for external API calls
builder.Services.AddHttpClient();

// 3. Repository Pattern + Unit of Work
builder.Services.AddRepositories();

// 4. Business Services
builder.Services.AddBusinessServices(builder.Configuration);

// 5. FluentValidation (works with DataAnnotations)
builder.Services.AddFluentValidationConfiguration();

// 6. Hybrid Authentication (Cookie for Web + JWT for Mobile)
builder.Services.AddHybridAuthenticationConfiguration(builder.Configuration);

// 7. Controllers with Validation Filter
builder.Services.AddValidationFilter();

// 8. CORS Configuration
builder.Services.AddCorsConfiguration(builder.Configuration);

// 9. Swagger/OpenAPI with detailed documentation
builder.Services.AddSwaggerConfiguration();

// 10. Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 11. Configure Kestrel for large file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

// 12. Configure Form Options for multipart uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
// 11. Add Auto Mapper
builder.Services.AddAutoMapper(typeof(TestTypeProfile), typeof(VenueLocationProfile));

// 12. Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

// ========================================
// Configure Middleware Pipeline
// ========================================

// 1. Exception handling (must be first)
app.UseExceptionMiddleware();

// 2. TraceId for request tracking
app.UseTraceId();

// 3. Swagger UI (available in all environments for testing)
app.UseSwaggerConfiguration();

// 3.1. Scalar - Đẹp nhất, hiện đại nhất (RECOMMENDED)
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Capstone API")
        .WithTheme(Scalar.AspNetCore.ScalarTheme.Purple)
        .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient)
        .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
});

// 4. HTTPS Redirection
app.UseHttpsRedirection();

// 5. CORS
app.UseCors("AllowAll");

// 6. Authentication (must be before Authorization)
app.UseAuthentication();

// 7. Authorization
app.UseAuthorization();

// 8. Map Controllers
app.MapControllers();

// ========================================
// Run Application
// ========================================

app.Logger.LogInformation("Application starting...");

app.Run();