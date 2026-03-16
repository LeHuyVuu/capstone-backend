using System.Text;
using System.Text.Json;
using capstone_backend.Api.Controllers;
using capstone_backend.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.VenueRecommendation.Api;

[ApiController]
[Route("api/venue-location")]
public class VenueLocationMeiliDirectController : BaseController
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VenueLocationMeiliDirectController> _logger;

    public VenueLocationMeiliDirectController(
        IConfiguration configuration,
        ILogger<VenueLocationMeiliDirectController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("search-direct")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> SearchDirect([FromBody] MeiliDirectSearchRequest? request)
    {
        request ??= new MeiliDirectSearchRequest();

        var host = Environment.GetEnvironmentVariable("MEILISEARCH_HOST")
                   ?? _configuration["Meilisearch:Host"]
                   ?? "http://134.209.108.208:7700";

        var apiKey = Environment.GetEnvironmentVariable("MEILI_MASTER_KEY")
                     ?? _configuration["Meilisearch:ApiKey"]
                     ?? "couplemood123";

        var indexUid = string.IsNullOrWhiteSpace(request.IndexUid)
            ? "venue_locations"
            : request.IndexUid.Trim();

        var body = new Dictionary<string, object?>
        {
            ["q"] = request.Q,
            ["offset"] = request.Offset,
            ["limit"] = request.Limit,
            ["filter"] = request.Filter,
            ["sort"] = request.Sort,
            ["attributesToRetrieve"] = request.AttributesToRetrieve,
            ["personalize"] = request.Personalize
        };

        if (request.Hybrid != null)
        {
            body["hybrid"] = request.Hybrid;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync($"{host.TrimEnd('/')}/indexes/{indexUid}/search", content);

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[MEILI DIRECT] Failed with status {StatusCode}: {ResponseBody}", (int)response.StatusCode, responseBody);
            return StatusCode(
                (int)response.StatusCode,
                ApiResponse<object>.ErrorData(responseBody, "Meilisearch direct search failed", (int)response.StatusCode, GetTraceId()));
        }

        object data;
        try
        {
            data = JsonSerializer.Deserialize<object>(responseBody) ?? responseBody;
        }
        catch
        {
            data = responseBody;
        }

        return OkResponse(data, "Meilisearch direct search success");
    }

    public sealed class MeiliDirectSearchRequest
    {
        public string IndexUid { get; set; } = "venue_locations";
        public string? Q { get; set; }
        public int Limit { get; set; } = 20;
        public int Offset { get; set; } = 0;
        public string? Filter { get; set; }
        public string[]? Sort { get; set; }
        public string[]? AttributesToRetrieve { get; set; } = new[] { "*" };
        public MeiliHybridPayload? Hybrid { get; set; }
        public MeiliPersonalizePayload? Personalize { get; set; }
    }

    public sealed class MeiliHybridPayload
    {
        public string? Embedder { get; set; }
        public double? SemanticRatio { get; set; }
    }

    public sealed class MeiliPersonalizePayload
    {
        public string? UserContext { get; set; }
    }
}
