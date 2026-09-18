using System.Diagnostics;
using System.Text.Json;

namespace ModularBatteries.Services;

/// <summary>
/// Manages the long-running `mojo run main.mojo` child process and routes
/// requests to it via stdin/stdout using the three-field tab-separated protocol:
///     MODULE TAB MODEL_ID TAB PROMPT
/// Responses are JSON-encoded:
///     RESULT TAB {"module":"...","model":"...","text":"..."}
/// </summary>
public sealed class MojoBridge : IDisposable
{
    private readonly string _root;
    private Process? _process;
    private bool _ready;

    public MojoBridge()
    {
        // Walk up from the build output directory to the repository root.
        _root = FindRepoRoot(AppContext.BaseDirectory)
                ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public bool IsRunning => _process is { HasExited: false } && _ready;

    public async Task<string> RouteAsync(string module, string modelId, string prompt)
    {
        try
        {
            await EnsureStartedAsync();
            if (_process is null) return LocalResponse(module, modelId, prompt);

            var line = $"{module}\t{modelId}\t{prompt.Replace('\n', ' ')}";
            await _process.StandardInput.WriteLineAsync(line);
            await _process.StandardInput.FlushAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var raw = await _process.StandardOutput.ReadLineAsync(cts.Token);
            if (raw is null) return LocalResponse(module, modelId, prompt);

            // Parse: RESULT\t{...json...}
            if (!raw.StartsWith("RESULT\t", StringComparison.Ordinal))
                return raw;

            var json = raw["RESULT\t".Length..];
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("text", out var t)
                ? t.GetString() ?? json
                : json;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ready = false;
            return LocalResponse(module, modelId, prompt);
        }
    }

    public async Task<bool> HealthAsync()
    {
        try
        {
            await EnsureStartedAsync();
            return IsRunning;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try { _process?.Kill(entireProcessTree: true); } catch { /* ignored */ }
        _process?.Dispose();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task EnsureStartedAsync()
    {
        if (_process is { HasExited: false } && _ready) return;

        _ready = false;
        _process?.Dispose();

        var psi = new ProcessStartInfo
        {
            FileName = "mojo",
            Arguments = "run main.mojo",
            WorkingDirectory = _root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        _process = Process.Start(psi);
        if (_process is null) return;

        // Wait for the READY sentinel (max 30 s)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        string? line;
        while ((line = await _process.StandardOutput.ReadLineAsync(cts.Token)) is not null)
        {
            if (line.Contains("MODULAR_BATTERIES_READY", StringComparison.Ordinal))
            {
                _ready = true;
                return;
            }
        }
    }

    private static string LocalResponse(string module, string model, string prompt) =>
        $"[Prototype mode — Mojo runtime not available]\n\n" +
        $"Module: {module}  |  Model: {model}\n\n" +
        $"Your prompt: {prompt}\n\n" +
        $"Install Mojo (modular.com) and run `mojo run main.mojo` from the repository root " +
        $"to activate the full orchestrator pipeline.";

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "main.mojo")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}