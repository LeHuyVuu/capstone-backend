using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Recommendation;
using capstone_backend.Data.Interfaces;
using Meilisearch;

namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Meilisearch service implementation for venue location query
/// </summary>
public partial class MeilisearchService : IMeilisearchService
{
    private readonly MeilisearchClient _meilisearchClient;
    private readonly IVenueLocationRepository _venueLocationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MeilisearchService> _logger;
    private readonly string _indexName;

    public MeilisearchService(
        IConfiguration configuration,
        IVenueLocationRepository venueLocationRepository,
        IUnitOfWork unitOfWork,
        ILogger<MeilisearchService> logger)
    {
        var host = Environment.GetEnvironmentVariable("MEILISEARCH_HOST") 
                   ?? configuration["Meilisearch:Host"] 
                   ?? "http://localhost:7700";
        var apiKey = Environment.GetEnvironmentVariable("MEILISEARCH_API_KEY") 
                     ?? configuration["Meilisearch:ApiKey"] 
                     ?? "masterKey123";
        _indexName = configuration["Meilisearch:IndexName"] ?? "venue_locations";

        _meilisearchClient = new MeilisearchClient(host, apiKey);
        _venueLocationRepository = venueLocationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;

        _logger.LogInformation("Meilisearch client initialized with host: {Host}, index: {Index}", host, _indexName);
    }
}
