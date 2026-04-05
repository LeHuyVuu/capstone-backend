using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace capstone_backend.Api.VenueRecommendation.Service;

/// <summary>
/// Static helpers to sync venue documents to an external Meilisearch server.
/// </summary>
public static class MeilisearchSyncDataUtil
{
    public const string DefaultHost = "http://134.209.108.208:7700";
    public const string DefaultSourceHost = "http://167.99.68.193:7700";
    private const string DefaultIndexName = "venue_locations";

    /// <summary>
    /// Sync documents to Meilisearch index with primary key "id".
    /// </summary>
    public static async Task<int> SyncDataAsync<T>(
        IEnumerable<T> documents,
        string indexName,
        string? host = null,
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        var payload = documents?.ToList() ?? new List<T>();
        if (payload.Count == 0)
            return 0;

        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("indexName is required", nameof(indexName));

        var targetHost = string.IsNullOrWhiteSpace(host) ? DefaultHost : host.Trim();
        var targetApiKey = apiKey ?? Environment.GetEnvironmentVariable("MEILI_MASTER_KEY") ?? string.Empty;

        using var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(targetApiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetApiKey);
        }

        var endpoint = $"{targetHost.TrimEnd('/')}/indexes/{indexName}/documents?primaryKey=id";
        using var response = await httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Meilisearch sync failed ({(int)response.StatusCode}): {responseBody}");
        }

        var taskUid = TryReadTaskUid(responseBody);
        if (taskUid.HasValue)
        {
            await WaitForTaskCompletionAsync(httpClient, targetHost, taskUid.Value, cancellationToken);
        }

        return payload.Count;
    }

    /// <summary>
    /// Sync a single document to Meilisearch index.
    /// </summary>
    public static Task<int> SyncOneAsync<T>(
        T document,
        string indexName,
        string? host = null,
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        return SyncDataAsync(new[] { document }, indexName, host, apiKey, cancellationToken);
    }

    /// <summary>
    /// Sync one venue document to target host by copying full document from source host.
    /// This follows the old one-venue sync behavior (full document upsert).
    /// </summary>
    public static async Task<int> SyncVenueByIdLikeOldAsync(
        int venueId,
        string indexName = DefaultIndexName,
        string? sourceHost = null,
        string? targetHost = null,
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        var srcHost = string.IsNullOrWhiteSpace(sourceHost)
            ? Environment.GetEnvironmentVariable("MEILISEARCH_HOST") ?? DefaultSourceHost
            : sourceHost.Trim();

        var target = string.IsNullOrWhiteSpace(targetHost) ? DefaultHost : targetHost.Trim();

        using var httpClient = CreateClient(apiKey);
        var sourceEndpoint = $"{srcHost.TrimEnd('/')}/indexes/{indexName}/documents/{venueId}";
        using var sourceResponse = await httpClient.GetAsync(sourceEndpoint, cancellationToken);
        var sourceBody = await sourceResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!sourceResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to load source document {venueId} from {srcHost} ({(int)sourceResponse.StatusCode}): {sourceBody}");
        }

        using var sourceJson = JsonDocument.Parse(sourceBody);
        var document = sourceJson.RootElement.Clone();
        return await SyncOneAsync(document, indexName, target, apiKey, cancellationToken);
    }

    private static HttpClient CreateClient(string? apiKey)
    {
        var targetApiKey = apiKey ?? Environment.GetEnvironmentVariable("MEILI_MASTER_KEY") ?? string.Empty;
        var httpClient = new HttpClient();

        if (!string.IsNullOrWhiteSpace(targetApiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetApiKey);
        }

        return httpClient;
    }

    private static long? TryReadTaskUid(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            if (root.TryGetProperty("taskUid", out var taskUidEl) && taskUidEl.ValueKind == JsonValueKind.Number)
                return taskUidEl.GetInt64();

            if (root.TryGetProperty("uid", out var uidEl) && uidEl.ValueKind == JsonValueKind.Number)
                return uidEl.GetInt64();
        }
        catch
        {
            // Ignore parse issues and skip task polling.
        }

        return null;
    }

    private static async Task WaitForTaskCompletionAsync(
        HttpClient httpClient,
        string host,
        long taskUid,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 40;
        const int delayMs = 250;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskEndpoint = $"{host.TrimEnd('/')}/tasks/{taskUid}";
            using var taskResponse = await httpClient.GetAsync(taskEndpoint, cancellationToken);
            var taskBody = await taskResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!taskResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Meilisearch task check failed ({(int)taskResponse.StatusCode}): {taskBody}");
            }

            using var taskJson = JsonDocument.Parse(taskBody);
            var status = taskJson.RootElement.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString()
                : null;

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Meilisearch task {taskUid} failed: {taskBody}");
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for Meilisearch task {taskUid} completion.");
    }
}
