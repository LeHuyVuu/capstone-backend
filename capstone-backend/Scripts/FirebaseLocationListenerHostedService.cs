using capstone_backend.Business.Interfaces;

namespace capstone_backend.Scripts;

public sealed class FirebaseLocationListenerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FirebaseLocationListenerHostedService> _logger;

    public FirebaseLocationListenerHostedService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<FirebaseLocationListenerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = Environment.GetEnvironmentVariable("FIREBASE_RTDB_BASE_URL")?.Trim();
        var authToken = Environment.GetEnvironmentVariable("FIREBASE_RTDB_AUTH_TOKEN")?.Trim();
        var coupleIdsRaw = Environment.GetEnvironmentVariable("FIREBASE_LOCATION_COUPLE_IDS")?.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("Firebase location listener disabled: FIREBASE_RTDB_BASE_URL is missing");
            return;
        }

        var coupleIds = ParseCoupleIds(coupleIdsRaw).ToList();
        if (coupleIds.Count == 0)
        {
            _logger.LogWarning("Firebase location listener disabled: FIREBASE_LOCATION_COUPLE_IDS is empty");
            return;
        }

        var options = BuildOptionsFromEnvironment();

        _logger.LogInformation(
            "Firebase location listener started for couples: {CoupleIds}",
            string.Join(",", coupleIds));

        var tasks = coupleIds
            .Select(coupleId => RunCoupleLoopAsync(baseUrl, authToken, coupleId, options, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task RunCoupleLoopAsync(
        string baseUrl,
        string? authToken,
        int coupleId,
        FirebaseLocationNotifierOptions options,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                var listener = new FirebaseLocationNotifier(httpClient, unitOfWork, options);
                await listener.ListenCoupleAsync(baseUrl, coupleId, authToken, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "Firebase stream ended unexpectedly for couple {CoupleId}, reconnecting...",
                        coupleId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Firebase stream error for couple {CoupleId}. Reconnecting in 5s...",
                    coupleId);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static IEnumerable<int> ParseCoupleIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            yield break;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var id) && id > 0)
                yield return id;
        }
    }

    private static FirebaseLocationNotifierOptions BuildOptionsFromEnvironment()
    {
        return new FirebaseLocationNotifierOptions
        {
            MinStepDistanceMeters = ReadDouble("FIREBASE_LOCATION_MIN_STEP_METERS", 30),
            NotificationCooldownSeconds = ReadInt("FIREBASE_LOCATION_COOLDOWN_SECONDS", 60),
            MinDistanceFromLastNotificationMeters = ReadDouble("FIREBASE_LOCATION_MIN_DISTANCE_FROM_LAST_NOTIFY_METERS", 100)
        };
    }

    private static int ReadInt(string key, int defaultValue)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : defaultValue;
    }

    private static double ReadDouble(string key, double defaultValue)
    {
        return double.TryParse(Environment.GetEnvironmentVariable(key), out var value) ? value : defaultValue;
    }
}
