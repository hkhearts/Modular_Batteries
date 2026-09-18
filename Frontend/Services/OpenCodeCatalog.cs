using System.Diagnostics;
using System.Text.Json;
using ModularBatteries.Models;

namespace ModularBatteries.Services;

/// <summary>
/// Discovers locally available AI models.
/// Tries (in order):
///   1. mojo run main.mojo catalog   — uses Mojo to query OpenCode
///   2. opencode models --json        — direct OpenCode CLI call
///   3. Hardcoded prototype list
/// </summary>
public sealed class OpenCodeCatalog
{
    private static readonly string[] FallbackIds =
    [
        "local/auto", "local/fast", "local/precise",
        "ollama/llama3", "ollama/codellama", "ollama/mistral",
        "ollama/phi3",   "ollama/deepseek-r1",
    ];

    private readonly string _repoRoot;

    public OpenCodeCatalog()
    {
        _repoRoot = FindRepoRoot(AppContext.BaseDirectory)
                    ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    public async Task<IReadOnlyList<ModelInfo>> DiscoverAsync()
    {
        // Strategy 1 — Mojo catalog sub-command
        var models = await TryMojoCatalogAsync();
        if (models.Count > 0) return models;

        // Strategy 2 — OpenCode CLI direct
        models = await TryOpenCodeDirectAsync();
        if (models.Count > 0) return models;

        // Strategy 3 — Fallback prototype list
        return FallbackModels();
    }

    // -----------------------------------------------------------------------
    private async Task<IReadOnlyList<ModelInfo>> TryMojoCatalogAsync()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "mojo",
                Arguments = "run main.mojo catalog",
                WorkingDirectory = _repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (proc is null) return [];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            return proc.ExitCode == 0 ? ParseJson(output) : [];
        }
        catch { return []; }
    }

    private static async Task<IReadOnlyList<ModelInfo>> TryOpenCodeDirectAsync()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "opencode",
                Arguments = "models --json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (proc is null) return [];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            return proc.ExitCode == 0 ? ParseJson(output) : [];
        }
        catch { return []; }
    }

    private static IReadOnlyList<ModelInfo> ParseJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return [];
            using var doc = JsonDocument.Parse(json);
            var list = new List<ModelInfo>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(id)) continue;
                var provider = item.TryGetProperty("provider", out var pEl) ? pEl.GetString() ?? "OpenCode" : "OpenCode";
                var detail   = item.TryGetProperty("detail",   out var dEl) ? dEl.GetString() ?? ""          : "";
                list.Add(new ModelInfo(id, provider, detail));
            }
            return list.Count > 0 ? list : [];
        }
        catch { return []; }
    }

    private static IReadOnlyList<ModelInfo> FallbackModels() =>
    [
        new("local/auto",         "Prototype", "OpenCode not detected"),
        new("local/fast",         "Prototype", "Local fallback — fast"),
        new("local/precise",      "Prototype", "Local fallback — precise"),
        new("ollama/llama3",      "Ollama",    "Meta Llama 3"),
        new("ollama/codellama",   "Ollama",    "Code Llama"),
        new("ollama/mistral",     "Ollama",    "Mistral 7B"),
        new("ollama/phi3",        "Ollama",    "Microsoft Phi-3"),
        new("ollama/deepseek-r1", "Ollama",    "DeepSeek-R1"),
    ];

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "main.mojo"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}