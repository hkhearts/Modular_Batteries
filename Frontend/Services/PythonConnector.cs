using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModularBatteries.Models;

namespace ModularBatteries.Services;

/// <summary>
/// Optional HTTP client that talks to the Python FastAPI connector running on
/// localhost:8765. When the service is not available all calls silently fail
/// and the caller falls back to the Mojo bridge.
/// </summary>
public sealed class PythonConnector : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://127.0.0.1:8765";

    public PythonConnector()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(5) };
    }

    // -----------------------------------------------------------------------
    // Health
    // -----------------------------------------------------------------------

    /// <summary>Returns true if the Python connector is reachable.</summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/health");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // -----------------------------------------------------------------------
    // Model catalog
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync()
    {
        try
        {
            var entries = await _http.GetFromJsonAsync<List<RemoteModelEntry>>("/models");
            if (entries is null || entries.Count == 0) return [];
            return entries
                .Select(e => new ModelInfo(e.Id, e.Provider, e.Detail))
                .ToList();
        }
        catch { return []; }
    }

    // -----------------------------------------------------------------------
    // Route
    // -----------------------------------------------------------------------

    /// <summary>
    /// Routes a prompt through the Python connector.
    /// Returns null if the connector is unavailable so callers can fall back.
    /// </summary>
    public async Task<string?> RouteAsync(string module, string model, string prompt, string workspace = "")
    {
        try
        {
            var payload = new { module, model, prompt, workspace };
            var resp = await _http.PostAsJsonAsync("/route", payload);
            if (!resp.IsSuccessStatusCode) return null;
            var result = await resp.Content.ReadFromJsonAsync<RouteResult>();
            return result?.Text;
        }
        catch { return null; }
    }

    public void Dispose() => _http.Dispose();

    // -----------------------------------------------------------------------
    // DTO helpers
    // -----------------------------------------------------------------------

    private sealed class RemoteModelEntry
    {
        [JsonPropertyName("id")]       public string Id       { get; init; } = "";
        [JsonPropertyName("provider")] public string Provider { get; init; } = "";
        [JsonPropertyName("detail")]   public string Detail   { get; init; } = "";
    }

    private sealed class RouteResult
    {
        [JsonPropertyName("text")]   public string Text   { get; init; } = "";
        [JsonPropertyName("source")] public string Source { get; init; } = "";
    }
}
