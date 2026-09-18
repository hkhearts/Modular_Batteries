using System.Diagnostics;
using System.Net.Http;

namespace ModularBatteries.Services;

/// <summary>
/// Automatically starts the Python FastAPI connector as a background child process.
/// No manual venv activation needed — uses the system python and pip directly.
/// </summary>
public sealed class ConnectorManager : IDisposable
{
    private static readonly string PythonDir =
        Path.GetFullPath(Path.Combine(FindRepoRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory, "Python"));

    public const string BaseUrl = "http://127.0.0.1:8765";

    private Process? _process;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private bool _started;

    // -----------------------------------------------------------------------
    // Public
    // -----------------------------------------------------------------------

    public bool IsRunning => _process is { HasExited: false };

    public async Task<bool> EnsureRunningAsync()
    {
        if (IsRunning) return true;
        if (_started && await IsHealthyAsync()) return true;

        // Try to start
        await StartAsync();
        _started = true;

        // Give it a moment to boot
        for (int i = 0; i < 15; i++)
        {
            await Task.Delay(400);
            if (await IsHealthyAsync()) return true;
        }
        return false;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{BaseUrl}/health");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // -----------------------------------------------------------------------
    // Private: start the connector
    // -----------------------------------------------------------------------

    private async Task StartAsync()
    {
        if (!Directory.Exists(PythonDir)) return;

        var connectorPy = Path.Combine(PythonDir, "connector.py");
        if (!File.Exists(connectorPy)) return;

        // Find python executable
        var python = await FindPythonAsync();
        if (python is null) return;

        // Ensure dependencies are installed
        await EnsureDepsAsync(python);

        // Start uvicorn
        _process?.Dispose();
        _process = Process.Start(new ProcessStartInfo
        {
            FileName               = python,
            Arguments              = $"\"{connectorPy}\"",
            WorkingDirectory       = PythonDir,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        });
    }

    private static async Task<string?> FindPythonAsync()
    {
        // 1. Check venv inside Python/
        var venvPython = Path.Combine(PythonDir, ".venv", "Scripts", "python.exe");
        if (File.Exists(venvPython)) return venvPython;

        // 2. Check system python3 / python
        foreach (var name in new[] { "python3", "python", "python3.exe", "python.exe" })
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName               = name,
                    Arguments              = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                });
                if (proc is null) continue;
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0) return name;
            }
            catch { /* continue */ }
        }
        return null;
    }

    private static async Task EnsureDepsAsync(string python)
    {
        var requirementsTxt = Path.Combine(PythonDir, "requirements.txt");
        if (!File.Exists(requirementsTxt)) return;
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName               = python,
                Arguments              = $"-m pip install -q -r \"{requirementsTxt}\"",
                WorkingDirectory       = PythonDir,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            });
            if (proc is null) return;
            await proc.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
        }
        catch { /* ignore */ }
    }

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

    public void Dispose()
    {
        _http.Dispose();
        try { _process?.Kill(true); } catch { }
        _process?.Dispose();
    }
}
