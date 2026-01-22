using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.ReDoc;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace capstone_backend.Extensions;

/// <summary>
/// Extension methods for configuring Swagger/OpenAPI
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Add Swagger with detailed configuration
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "CoupleMood Backend API",
                Description = "RESTful API for Capstone project",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "couplemood.system@gmail.com",
                    Url = new Uri("https://github.com/LeHuyVuu/capstone-backend")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Cookie Authentication Security Scheme
            options.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = "CapstoneAuth",
                Description = "Cookie-based authentication. Login via /api/auth/login to receive authentication cookie.",
                Scheme = "Cookie"
            });

            // Apply security requirement globally
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Cookie"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments for detailed documentation
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // Custom operation filters for better documentation
            options.EnableAnnotations();

            // Schema filters for better model documentation
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            // Order actions by HTTP method
            options.OrderActionsBy(apiDesc =>
            {
                var order = apiDesc.HttpMethod switch
                {
                    "GET" => 1,
                    "POST" => 2,
                    "PUT" => 3,
                    "PATCH" => 4,
                    "DELETE" => 5,
                    _ => 6
                };
                return $"{order}_{apiDesc.RelativePath}";
            });
        });

        return services;
    }

    // Sử dụng Swagger UI và Redoc
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger();
        
        // Swagger UI - giao diện cũ, có thể test API
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Capstone API v1");
            options.RoutePrefix = "swagger"; // Truy cập tại /swagger
            options.DocumentTitle = "Capstone API - Swagger UI";
            
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.EnableTryItOutByDefault();
            options.ConfigObject.PersistAuthorization = true;
        });

        // Redoc - giao diện đẹp hơn, dễ đọc hơn (chỉ xem, không test được)
        app.UseReDoc(options =>
        {
            options.SpecUrl = "/swagger/v1/swagger.json";
            options.RoutePrefix = "redoc";
            options.DocumentTitle = "Capstone API - Redoc";
            options.ConfigObject.HideDownloadButton = false;
            options.ConfigObject.ExpandResponses = "200,201";
        });

        return app;
    }
}

/// <summary>
/// Filter để hỗ trợ file upload trong Swagger UI
/// </summary>
internal class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Tìm các parameter là IFormFile
        var formFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
            .ToList();

        if (!formFileParams.Any())
            return;

        // Xóa các parameter cũ
        operation.Parameters?.Clear();

        // Thiết lập request body là multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formFileParams.ToDictionary(
                            p => p.Name,
                            p => new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = p.ModelMetadata?.Description ?? "File to upload"
                            }
                        ),
                        Required = formFileParams
                            .Where(p => p.IsRequired)
                            .Select(p => p.Name)
                            .ToHashSet()
                    }
                }
            }
        };
    }
}
