using System.Diagnostics;
using System.Text.Json;
using ModularBatteries.Models;

namespace ModularBatteries.Services;

/// <summary>
/// Discovers models from both Ollama (via `ollama list`) and OpenCode (via `opencode models --json`).
/// Falls back gracefully when either tool is unavailable.
/// </summary>
public static class ModelDiscoveryService
{
    public static async Task<IReadOnlyList<DiscoveredModel>> DiscoverAllAsync()
    {
        var results = new List<DiscoveredModel>();

        var ollamaTask  = DiscoverOllamaAsync();
        var opencodeTask = DiscoverOpenCodeAsync();

        await Task.WhenAll(ollamaTask, opencodeTask);

        results.AddRange(await ollamaTask);
        results.AddRange(await opencodeTask);

        if (results.Count == 0)
        {
            // Absolute fallback so the UI is never empty
            results.AddRange(FallbackModels());
        }
        return results;
    }

    // -----------------------------------------------------------------------
    // Ollama
    // -----------------------------------------------------------------------

    public static async Task<IReadOnlyList<DiscoveredModel>> DiscoverOllamaAsync()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName               = "ollama",
                Arguments              = "list",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            if (proc is null) return [];

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            if (proc.ExitCode != 0) return [];

            return ParseOllamaList(output);
        }
        catch { return []; }
    }

    private static IReadOnlyList<DiscoveredModel> ParseOllamaList(string raw)
    {
        // Format: NAME  ID  SIZE  MODIFIED
        var models = new List<DiscoveredModel>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;
            var name = parts[0]; // e.g. "llama3:latest"
            var size = parts.Length >= 3 ? $"{parts[2]} {(parts.Length > 3 ? parts[3] : "")}" : "";
            var display = name.Contains(':') ? name[..name.IndexOf(':')] : name;
            models.Add(new DiscoveredModel(name, display, "Ollama", size.Trim()));
        }
        return models;
    }

    // -----------------------------------------------------------------------
    // OpenCode
    // -----------------------------------------------------------------------

    public static async Task<IReadOnlyList<DiscoveredModel>> DiscoverOpenCodeAsync()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName               = "opencode",
                Arguments              = "models",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            });
            if (proc is null) return [];

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);

            // Try JSON parse first
            var json = TryParseOpenCodeJson(output);
            if (json.Count > 0) return json;

            // Parse as plain text list
            return ParseOpenCodeText(output);
        }
        catch { return []; }
    }

    private static IReadOnlyList<DiscoveredModel> TryParseOpenCodeJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var list = new List<DiscoveredModel>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id",   out var idEl)   ? idEl.GetString()   : null;
                var nm = item.TryGetProperty("name", out var nmEl)   ? nmEl.GetString()   : id;
                if (string.IsNullOrWhiteSpace(id)) continue;
                list.Add(new DiscoveredModel(id, nm ?? id, "OpenCode", ""));
            }
            return list;
        }
        catch { return []; }
    }

    private static IReadOnlyList<DiscoveredModel> ParseOpenCodeText(string raw)
    {
        var list = new List<DiscoveredModel>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("Error", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("opencode", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.Contains(' ') && line.Length < 80)
            {
                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                list.Add(new DiscoveredModel(parts[0], parts[0], "OpenCode", parts.Length > 1 ? parts[1] : ""));
            }
            else if (line.Length > 0 && line.Length < 80 && !line.Contains('\t'))
            {
                list.Add(new DiscoveredModel(line, line, "OpenCode", ""));
            }
        }
        return list;
    }

    // -----------------------------------------------------------------------
    // Fallback
    // -----------------------------------------------------------------------

    public static IReadOnlyList<DiscoveredModel> FallbackModels() =>
    [
        new("llama3:latest",       "llama3",         "Ollama",    "Local fallback"),
        new("deepseek-coder:6.7b", "deepseek-coder", "Ollama",    "Local fallback"),
        new("local/auto",          "Auto",           "Prototype", "No tools detected"),
    ];
}
