using capstone_backend.Api.Middleware;
using capstone_backend.Extensions;
using DotNetEnv;
using Scalar.AspNetCore;

// Load environment variables from .env file (if exists)
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Configure Services (Dependency Injection)
// ========================================

// 1. Database Context with PostgreSQL + EF Core
builder.Services.AddDatabaseContext(builder.Configuration);

// 2. Repository Pattern + Unit of Work
builder.Services.AddRepositories();

// 3. Business Services
builder.Services.AddBusinessServices();

// 4. FluentValidation (works with DataAnnotations)
builder.Services.AddFluentValidationConfiguration();

// 5. Cookie-based Authentication & Authorization
builder.Services.AddCookieAuthenticationConfiguration(builder.Configuration);

// 6. Controllers with Validation Filter
builder.Services.AddValidationFilter();

// 7. CORS Configuration
builder.Services.AddCorsConfiguration(builder.Configuration);

// 8. Swagger/OpenAPI with detailed documentation
builder.Services.AddSwaggerConfiguration();

// 9. Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
        .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient);
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
app.Logger.LogInformation("🚀 Scalar (recommended): http://localhost:5224/scalar");
app.Logger.LogInformation("📘 Swagger UI: http://localhost:5224/swagger");
app.Logger.LogInformation("📖 Redoc: http://localhost:5224/redoc");
app.Logger.LogInformation("API: /api");

app.Run();